using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

using BVE5Language.Ast;
using BVE5Language.TypeSystem;

namespace BVE5Language.Resolver
{
	/// <summary>
	/// Traverses the DOM and resolves expressions.
	/// </summary>
	/// <remarks>
	/// The ResolveVisitor does two jobs at the same time: it tracks the resolve context (properties on CSharpResolver)
	/// and it resolves the expressions visited.
    /// To allow using the context tracking without having to resolve every expression in the file (e.g. when you want to resolve
    /// only a single node deep within the DOM), you can use the <see cref="IResolveVisitorNavigator"/> interface.
    /// The navigator allows you to switch the between scanning mode and resolving mode.
    /// In scanning mode, the context is tracked (local variables registered etc.), but nodes are not resolved.
    /// While scanning, the navigator will get asked about every node that the resolve visitor is about to enter.
    /// This allows the navigator whether to keep scanning, whether switch to resolving mode, or whether to completely skip the
    /// subtree rooted at that node.
    /// 
    /// In resolving mode, the context is tracked and nodes will be resolved.
    /// The resolve visitor may decide that it needs to resolve other nodes as well in order to resolve the current node.
    /// In this case, those nodes will be resolved automatically, without asking the navigator interface.
    /// For child nodes that are not essential to resolving, the resolve visitor will switch back to scanning mode (and thus will
    /// ask the navigator for further instructions).
    /// 
    /// Moreover, there is the <c>ResolveAll</c> mode - it works similar to resolving mode, but will not switch back to scanning mode.
    /// The whole subtree will be resolved without notifying the navigator.
	/// </remarks>
	internal sealed class ResolveVisitor : IAstWalker<ResolveResult>
	{
		static readonly ResolveResult RrrorResult = ErrorResolveResult.UnknownError;
		
		BVE5Resolver resolver;
		readonly BVE5UnresolvedFile unresolved_file;
		readonly string toplevel_type_name;
		readonly Dictionary<AstNode, ResolveResult> resolve_result_cache = new Dictionary<AstNode, ResolveResult>();
		readonly Dictionary<AstNode, BVE5Resolver> resolver_before_dict = new Dictionary<AstNode, BVE5Resolver>();
		readonly Dictionary<AstNode, BVE5Resolver> resolver_after_dict = new Dictionary<AstNode, BVE5Resolver>();

        IResolveVisitorNavigator navigator;
		bool resolver_enabled;
		internal CancellationToken cancellation_token;

        static readonly IResolveVisitorNavigator SkipAllNavigator = new ConstantModeResolveVisitorNavigator(ResolveVisitorNavigationMode.Skip, null);
		
		#region Constructor
		/// <summary>
		/// Creates a new ResolveVisitor instance.
		/// </summary>
		public ResolveVisitor(BVE5Resolver inputResolver, BVE5UnresolvedFile unresolvedFile, string toplevelTypeName = null)
		{
			if(inputResolver == null)
				throw new ArgumentNullException("inputResolver");

			resolver = inputResolver;
			unresolved_file = unresolvedFile;
			toplevel_type_name = toplevelTypeName;
			navigator = SkipAllNavigator;
		}

        internal void SetNavigator(IResolveVisitorNavigator navigator)
        {
            this.navigator = navigator ?? SkipAllNavigator;
        }

        ResolveResult VoidResult{
            get{
        		return new ResolveResult(resolver.Compilation.FindType(BVEPrimitiveTypeCode.None));
            }
        }
		#endregion

		#region ResetContext
		/// <summary>
		/// Resets the visitor to the stored position, runs the action, and then reverts the visitor to the previous position.
		/// </summary>
		void ResetContext(BVE5Resolver storedContext, Action action)
		{
			var old_resolver_enabled = resolver_enabled;
			var old_resolver = this.resolver;
			try{
				resolver_enabled = false;
				this.resolver = storedContext;
				
				action();
			}finally{
				resolver_enabled = old_resolver_enabled;
				this.resolver = old_resolver;
			}
		}
		#endregion

		void StoreCurrentState(AstNode node)
		{
			// It's possible that we re-visit an expression that we scanned over earlier,
			// so we might have to overwrite an existing state.
			
			resolver_before_dict[node] = resolver;
		}
		
		void StoreResult(AstNode node, ResolveResult result)
		{
			Debug.Assert(result != null);
			Log.WriteLine("Resolved '{0}' to {1}", node, result);
			Debug.Assert(!BVE5AstResolver.IsUnresolvableNode(node));
			// The state should be stored before the result would be.
			Debug.Assert(resolver_before_dict.ContainsKey(node));
			// Don't store results twice.
			Debug.Assert(!resolve_result_cache.ContainsKey(node));
			resolve_result_cache[node] = result;
		}
		
		#region Scan / Resolve
		/// <summary>
		/// Scans the AST rooted at the given node.
		/// </summary>
		public void Scan(AstNode node)
		{
			if(node == null)
				return;

			// don't Scan again if the node was already resolved
			if(resolve_result_cache.ContainsKey(node)){
				// Restore state change caused by this node:
				BVE5Resolver new_resolver;
				if(resolver_after_dict.TryGetValue(node, out new_resolver))
					resolver = new_resolver;

				return;
			}

            var mode = navigator.Scan(node);
            switch(mode){
            case ResolveVisitorNavigationMode.Skip:
                StoreCurrentState(node);
                break;

            case ResolveVisitorNavigationMode.Scan:
                bool old_resolver_enabled = resolver_enabled;
                var oldResolver = resolver;
                resolver_enabled = false;
                StoreCurrentState(node);
                ResolveResult result = node.AcceptWalker(this);
                
                if(result != null){
                    // If the node was resolved, store the result even though it wasn't requested.
                    // This is necessary so that Visit-methods that decide to always resolve are
                    // guaranteed to get called only once.
                    // This is used for lambda registration.
                    StoreResult(node, result);
                    if(resolver != oldResolver){
                        // The node changed the resolver state:
                        resolver_after_dict.Add(node, resolver);
                    }
                    cancellation_token.ThrowIfCancellationRequested();
                }
                resolver_enabled = old_resolver_enabled;
                break;

            case ResolveVisitorNavigationMode.Resolve:
                Resolve(node);
                break;

            default:
                throw new InvalidOperationException("Invalid value for ResolveVisitorNavigationMode");
            }
		}
		
		/// <summary>
		/// Equivalent to 'Scan', but also resolves the node at the same time.
		/// This method should be only used if the BVE5Resolver passed to the ResolveVisitor was manually set
		/// to the correct state.
		/// Otherwise, use <c>resolver.Scan(syntaxTree); var result = resolver.GetResolveResult(node);</c>
		/// instead.
		/// --
		/// This method is now internal, because it is difficult to use correctly.
		/// Users of the public API should use Scan()+GetResolveResult() instead.
		/// </summary>
		internal ResolveResult Resolve(AstNode node)
		{
			if(node == null)
				return RrrorResult;

			bool oldResolverEnabled = resolver_enabled;
			resolver_enabled = true;
			ResolveResult result;

			if(!resolve_result_cache.TryGetValue(node, out result)){
				cancellation_token.ThrowIfCancellationRequested();
				StoreCurrentState(node);
				var oldResolver = resolver;
				result = node.AcceptWalker(this) ?? RrrorResult;
				StoreResult(node, result);
				if(resolver != oldResolver){
					// The node changed the resolver state:
					resolver_after_dict.Add(node, resolver);
				}
			}
			resolver_enabled = oldResolverEnabled;
			return result;
		}
		#endregion

		/// <summary>
		/// Gets the resolve result for the specified node.
		/// If the node was not resolved by the navigator, this method will resolve it.
		/// </summary>
		public ResolveResult GetResolveResult(AstNode node)
		{
			Debug.Assert(!BVE5AstResolver.IsUnresolvableNode(node));
			
			ResolveResult result;
			if(resolve_result_cache.TryGetValue(node, out result))
				return result;
			
			AstNode parent;
			BVE5Resolver stored_resolver = GetPreviouslyScannedContext(node, out parent);
			ResetContext(
				stored_resolver,
				() => {
                    navigator = new NodeListResolveVisitorNavigator(node);
				    Debug.Assert(!resolver_enabled);
				    Scan(parent);
                    navigator = SkipAllNavigator;
                });
			
			return resolve_result_cache[node];
		}

		BVE5Resolver GetPreviouslyScannedContext(AstNode node, out AstNode parent)
		{
			parent = node;
			BVE5Resolver stored_resolver;
			while(!resolver_before_dict.TryGetValue(parent, out stored_resolver)){
				parent = parent.Parent;
				if(parent == null){
					throw new InvalidOperationException("Could not find a resolver state for any parent of the specified node." +
					                                    "Are you trying to resolve a node that is not a descendant of the BVE5AstResolver's root node?");
				}
			}
			return stored_resolver;
		}

        void ScanChildren(AstNode parent)
        {
            for(AstNode child = parent.FirstChild; child != null; child = child.NextSibling)
                Scan(child);
        }

		#region AstWalker members
		public ResolveResult Walk(BinaryExpression binary)
		{
			return null; //TODO: implement it
		}
		
		public ResolveResult Walk(UnaryExpression unary)
		{
			return null; //TODO: implement it
		}
		
		public ResolveResult Walk(DefinitionExpression def)
		{
			return null; //TODO: implement it
		}
		
		public ResolveResult Walk(IndexerExpression indexingExpr)
		{
            ResolveResult target_rr = indexingExpr.Target.AcceptWalker(this);
            if(target_rr != null){
            	StoreCurrentState(indexingExpr);
            	var result = resolver.ResolveIndexer(target_rr, indexingExpr.Index.Value.ToString());
                StoreResult(indexingExpr, result);
                return result;
            }

			return null;
		}

        #region Invocation
        public ResolveResult Walk(InvocationExpression invocation)
		{
            var mre = invocation.Target as MemberReferenceExpression;
            var identifier = mre.Target as Identifier;
            
            if(identifier != null){
            	StoreCurrentState(identifier);
                StoreCurrentState(mre);
                
                var id_rr = resolver.ResolveTypeName(identifier.Name);
                var target_rr = ResolveMemberReferenceOnGivenTarget(id_rr, mre);
                Log.WriteLine("Member reference '{0}' on potentially-ambiguous simple-name was resolved to {1}", mre, target_rr);
                StoreResult(mre, target_rr);
                
                Log.WriteLine("Simple name '{0}' was resolved to {1}", identifier, id_rr);
                StoreResult(identifier, id_rr);
                return ResolveInvocationOnGivenTarget(target_rr, invocation);
            }else{	//regular code path
            	if(resolver_enabled){
            		var target = Resolve(invocation.Target);
            		return ResolveInvocationOnGivenTarget(target, invocation);
            	}else{
            		ScanChildren(invocation);
            		return null;
            	}
            }
		}
        
        ResolveResult ResolveInvocationOnGivenTarget(ResolveResult target, InvocationExpression invocation)
        {
        	ResolveResult[] args = GetArguments(invocation.Arguments);
        	return resolver.ResolveInvocation(target, args);
        }

        /// <summary>
		/// Gets and resolves the arguments.
		/// </summary>
        ResolveResult[] GetArguments(IEnumerable<Expression> argExpressions)
        {
            ResolveResult[] arguments = new ResolveResult[argExpressions.Count()];
            int i = 0;
            foreach(AstNode argument in argExpressions)
                arguments[i++] = Resolve(argument);
            
            return arguments;
        }
        #endregion

        public ResolveResult Walk(LiteralExpression literal)
		{
            return resolver.ResolvePrimitive(literal.Value);
		}

        #region Member reference
		public ResolveResult Walk(MemberReferenceExpression memRef)
		{
			var ident = memRef.Target as Identifier;
			if(ident != null){
				StoreCurrentState(ident);
				var target = resolver.ResolveTypeName(ident.Name);
				StoreResult(ident, target);
				Log.WriteLine("Simple name '{0}' is resolved to {1}", ident.GetText(), target);
				return ResolveMemberReferenceOnGivenTarget(target, memRef);
			}else{
				if(resolver_enabled){
					var target = Resolve(memRef.Target);
					return ResolveMemberReferenceOnGivenTarget(target, memRef);
				}else{
					ScanChildren(memRef);
					return null;
				}
			}
		}
		
		ResolveResult ResolveMemberReferenceOnGivenTarget(ResolveResult target, MemberReferenceExpression memRef)
		{
			StoreCurrentState(memRef.Reference);
			var result = resolver.ResolveMemberAccess(target, memRef.Reference.Name);
			StoreResult(memRef.Reference, result);
			return result;
		}
		#endregion
		
		#region Statement types
		public ResolveResult Walk(LetStatement letStmt)
		{
			return null; //TODO: implement it
		}

		public ResolveResult Walk(SectionStatement secStmt)
		{
			return null; //TODO: implement it
		}
		
		public ResolveResult Walk(Statement stmt)
		{
            var child_rr = stmt.Expr.AcceptWalker(this);	//manually call the AcceptWalker method so that literal expressions that is the only child of statements
            StoreCurrentState(stmt.Expr);					//will be resolved to position statements
            
            if(child_rr is ConstantResolveResult){  //Statements consisting only of a literal expression are considered to representing a route location
            	var result = (child_rr.ConstantValue is int) ? resolver.ResolvePositionStatement((int)child_rr.ConstantValue, child_rr.Type) :
            		new ErrorResolveResult(child_rr.Type, "A position statement must consist only of an integer literal", stmt.StartLocation);
                StoreResult(stmt.Expr, result);
                return result;
            }else{
            	return null;
            }
		}
		#endregion

		public ResolveResult Walk(SyntaxTree unit)
		{
            BVE5Resolver previous_resolver = resolver;
            try{
            	if(unresolved_file != null)
            		resolver = resolver.WithCurrentTypeDefinition(toplevel_type_name);

            	ScanChildren(unit);
                return VoidResult;
            }
            finally{
                resolver = previous_resolver;
            }
		}

		public ResolveResult Walk(TimeFormatLiteral timeLiteral)
		{
			StoreCurrentState(timeLiteral);
			var rr = resolver.ResolveTimeLiteral(timeLiteral);
			StoreResult(timeLiteral, rr);
			return rr;
		}

		#region Empty implementation
        public ResolveResult Walk(Identifier ident)
        {
        	return null;
        }
        
        public ResolveResult Walk(SequenceExpression sequence)
		{
			return null;
		}
        #endregion
        #endregion
    }
}

