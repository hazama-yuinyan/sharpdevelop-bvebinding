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
		private static readonly ResolveResult errorResult = ErrorResolveResult.UnknownError;
		
		private BVE5Resolver resolver;
		private readonly BVE5UnresolvedFile unresolved_file;
		private readonly Dictionary<AstNode, ResolveResult> resolveResultCache = new Dictionary<AstNode, ResolveResult>();
		private readonly Dictionary<AstNode, BVE5Resolver> resolverBeforeDict = new Dictionary<AstNode, BVE5Resolver>();
		private readonly Dictionary<AstNode, BVE5Resolver> resolverAfterDict = new Dictionary<AstNode, BVE5Resolver>();

        IResolveVisitorNavigator navigator;
		bool resolver_enabled;
		internal CancellationToken cancellation_token;

        static readonly IResolveVisitorNavigator skipAllNavigator = new ConstantModeResolveVisitorNavigator(ResolveVisitorNavigationMode.Skip, null);
		
		#region Constructor
		/// <summary>
		/// Creates a new ResolveVisitor instance.
		/// </summary>
		public ResolveVisitor(BVE5Resolver inputResolver, BVE5UnresolvedFile unresolvedFile)
		{
			if(inputResolver == null)
				throw new ArgumentNullException("inputResolver");

			resolver = inputResolver;
			unresolved_file = unresolvedFile;
			navigator = skipAllNavigator;
		}

        internal void SetNavigator(IResolveVisitorNavigator navigator)
        {
            this.navigator = navigator ?? skipAllNavigator;
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
			var oldResolverEnabled = resolver_enabled;
			var oldResolver = this.resolver;
			try{
				resolver_enabled = false;
				this.resolver = storedContext;
				
				action();
			}finally{
				resolver_enabled = oldResolverEnabled;
				this.resolver = oldResolver;
			}
		}
		#endregion

		private void StoreCurrentState(AstNode node)
		{
			// It's possible that we re-visit an expression that we scanned over earlier,
			// so we might have to overwrite an existing state.
			
			resolverBeforeDict[node] = resolver;
		}
		
		private void StoreResult(AstNode node, ResolveResult result)
		{
			Debug.Assert(result != null);
			Log.WriteLine("Resolved '{0}' to {1}", node, result);
			Debug.Assert(!BVE5AstResolver.IsUnresolvableNode(node));
			// The state should be stored before the result is.
			Debug.Assert(resolverBeforeDict.ContainsKey(node));
			// Don't store results twice.
			Debug.Assert(!resolveResultCache.ContainsKey(node));
			resolveResultCache[node] = result;
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
			if(resolveResultCache.ContainsKey(node)){
				// Restore state change caused by this node:
				BVE5Resolver new_resolver;
				if(resolverAfterDict.TryGetValue(node, out new_resolver))
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
                        resolverAfterDict.Add(node, resolver);
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
				return errorResult;

			bool oldResolverEnabled = resolver_enabled;
			resolver_enabled = true;
			ResolveResult result;

			if(!resolveResultCache.TryGetValue(node, out result)){
				cancellation_token.ThrowIfCancellationRequested();
				StoreCurrentState(node);
				var oldResolver = resolver;
				result = node.AcceptWalker(this) ?? errorResult;
				StoreResult(node, result);
				if(resolver != oldResolver){
					// The node changed the resolver state:
					resolverAfterDict.Add(node, resolver);
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
			if(resolveResultCache.TryGetValue(node, out result))
				return result;
			
			AstNode parent;
			BVE5Resolver stored_resolver = GetPreviouslyScannedContext(node, out parent);
			ResetContext(
				stored_resolver,
				() => {
                    navigator = new NodeListResolveVisitorNavigator(node);
				    Debug.Assert(!resolver_enabled);
				    Scan(parent);
                    navigator = skipAllNavigator;
                });
			
			return resolveResultCache[node];
		}

		BVE5Resolver GetPreviouslyScannedContext(AstNode node, out AstNode parent)
		{
			parent = node;
			BVE5Resolver stored_resolver;
			while(!resolverBeforeDict.TryGetValue(parent, out stored_resolver)){
				parent = parent.Parent;
				if(parent == null)
					throw new InvalidOperationException("Could not find a resolver state for any parent of the specified node. Are you trying to resolve a node that is not a descendant of the CSharpAstResolver's root node?");
			}
			return stored_resolver;
		}

        private void ScanChildren(AstNode parent)
        {
            for(AstNode child = parent.FirstChild; child != null; child = child.NextSibling)
                Scan(child);
        }

		#region AstWalker members
		public ResolveResult Walk(IndexerExpression node)
		{
            ResolveResult target_rr = node.Target.AcceptWalker(this);
            if(target_rr != null){
            	StoreCurrentState(node);
                var result = resolver.ResolveIndexer(target_rr, node.Index.Name);
                StoreResult(node, result);
                return result;
            }

			return null;
		}

        #region Invocation
        public ResolveResult Walk(InvocationExpression node)
		{
            ResolveResult target_rr = node.Target.AcceptWalker(this);
            if(target_rr != null && target_rr is MethodGroupResolveResult){
                StoreCurrentState(node);
                ResolveResult[] args = GetArguments(node.Arguments);
                var result = resolver.ResolveInvocation(target_rr, args);
                return result;
            }else{
                return new ErrorResolveResult(SpecialType.UnknownType, "The target expression doesn't seem to be an invocable.", node.StartLocation);
            }
		}

        /// <summary>
		/// Gets and resolves the arguments.
		/// </summary>
        private ResolveResult[] GetArguments(IEnumerable<Expression> argExpressions)
        {
            ResolveResult[] arguments = new ResolveResult[argExpressions.Count()];
            int i = 0;
            foreach(AstNode argument in argExpressions)
                arguments[i++] = Resolve(argument);
            
            return arguments;
        }
        #endregion

        public ResolveResult Walk(LiteralExpression node)
		{
            return resolver.ResolvePrimitive(node.Value);
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
		
		private ResolveResult ResolveMemberReferenceOnGivenTarget(ResolveResult target, MemberReferenceExpression memRef)
		{
			return resolver.ResolveMemberAccess(target, memRef.Reference.Name);
		}
		#endregion

		public ResolveResult Walk(Statement stmt)
		{
            var child_rr = stmt.Expr.AcceptWalker(this);
            if(child_rr is ConstantResolveResult){  //Statements consisting only of a literal expression are considered to representing a route location
                if(!(child_rr.ConstantValue is int))
                    return new ErrorResolveResult(child_rr.Type, "A position statement must consist only of an integer literal", stmt.StartLocation);

                return resolver.ResolvePositionStatement((int)child_rr.ConstantValue, child_rr.Type);
            }
			return null;
		}

		public ResolveResult Walk(SyntaxTree unit)
		{
            BVE5Resolver previous_resolver = resolver;
            try{
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
        #endregion
        #endregion
    }
}

