using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.TypeSystem;
using BVE5Language.Ast;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace BVE5Language.TypeSystem
{
	/// <summary>
	/// Produces type and member definitions from the DOM.
	/// </summary>
	public class TypeSystemConvertWalker : DepthFirstAstWalker<IUnresolvedEntity>
	{
		readonly BVE5UnresolvedFile unresolved_file;
		BVE5FileKind target_file_kind;
		List<string> cur_member_names;
		Dictionary<string, List<string>> member_name_defs;
		
		/// <summary>
		/// Creates a new TypeSystemConvertVisitor and initializes it with a given context.
		/// </summary>
		/// <param name="unresolvedFile">The parsed file to which members should be added.</param>
		/// <param name="currentUsingScope">The current using scope.</param>
		/// <param name="currentTypeDefinition">The current type definition.</param>
		public TypeSystemConvertWalker(BVE5UnresolvedFile unresolvedFile)
		{
			if(unresolvedFile == null)
				throw new ArgumentNullException("unresolvedFile");

			this.unresolved_file = unresolvedFile;
		}
		
		public BVE5UnresolvedFile UnresolvedFile {
			get { return unresolved_file; }
		}
		
		DomRegion MakeRegion(TextLocation start, TextLocation end)
		{
			return new DomRegion(unresolved_file.FileName, start.Line, start.Column, end.Line, end.Column);
		}
		
		DomRegion MakeRegion(AstNode node)
		{
			if(node == null)
				return node.GetRegion();
			else
				return MakeRegion(node.StartLocation, node.EndLocation);
		}
		
		IUnresolvedMember CreateField(IUnresolvedTypeDefinition declaringType, string literal)
		{
			var field = new DefaultUnresolvedField(declaringType, literal);
			return field;
		}
		
		#region DepthFirstAstWalker implementation
		public override IUnresolvedEntity Walk(SyntaxTree unit)
		{
			unresolved_file.Errors = unit.Errors;
			target_file_kind = unit.Kind;
			if(cur_member_names == null && unit.Kind != BVE5FileKind.RouteFile)
				cur_member_names = new List<string>();
			else if(unit.Kind == BVE5FileKind.RouteFile)
				member_name_defs = new Dictionary<string, List<string>>();
			
			base.Walk(unit);
			
			if(cur_member_names != null){
				var type_def = new DefaultUnresolvedTypeDefinition("global", FileKindHelper.GetTypeNameFromFileKind(unit.Kind));
				foreach(var name in cur_member_names.Distinct())
					type_def.Members.Add(CreateField(type_def, name));
				
				unresolved_file.TopLevelTypeDefinitions.Add(type_def);
			}else{
				foreach(KeyValuePair<string, List<string>> members in member_name_defs){
					var type_def = new DefaultUnresolvedTypeDefinition("global", members.Key);
					foreach(var name in members.Value.Distinct())
						type_def.Members.Add(CreateField(type_def, name));
					
					unresolved_file.TopLevelTypeDefinitions.Add(type_def);
				}
			}
			return null;
		}

		public override IUnresolvedEntity Walk(Identifier ident)
		{
			return base.Walk(ident);
		}

		public override IUnresolvedEntity Walk(IndexerExpression indexerExpr)
		{
			if(target_file_kind == BVE5FileKind.RouteFile){
				var type_ident = indexerExpr.Target as Identifier;
				if(type_ident != null){
					if(!member_name_defs.ContainsKey(type_ident.Name))
						member_name_defs.Add(type_ident.Name, new List<string>());
					
					member_name_defs[type_ident.Name].Add(indexerExpr.Index.Value.ToString());
				}
			}
			return base.Walk(indexerExpr);
		}

		public override IUnresolvedEntity Walk(InvocationExpression invoke)
		{
			if(target_file_kind != BVE5FileKind.RouteFile){
				var key_literal = invoke.Arguments.First() as LiteralExpression;
				if(key_literal != null)
					cur_member_names.Add(key_literal.Value.ToString());
			}
			return base.Walk(invoke);
		}

		public override IUnresolvedEntity Walk(MemberReferenceExpression memRef)
		{
			return memRef.Target.AcceptWalker(this);
		}

		public override IUnresolvedEntity Walk(Statement stmt)
		{
			if(stmt.Expr is InvocationExpression)
				stmt.Expr.AcceptWalker(this);
			
			return null;
		}
		#endregion
	}
}

