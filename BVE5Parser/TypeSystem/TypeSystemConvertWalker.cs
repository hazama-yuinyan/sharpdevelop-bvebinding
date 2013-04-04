using System;
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
		DefaultUnresolvedTypeDefinition type_def;
		
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
		
		#region DepthFirstAstWalker implementation
		public override IUnresolvedEntity Walk(SyntaxTree unit)
		{
			unresolved_file.Errors = unit.Errors;
			if(type_def == null)
				type_def = new DefaultUnresolvedTypeDefinition("global", FileKindHelper.GetTypeNameFromFileKind(unit.Kind));
			
			return base.Walk(unit);
		}

		/*public IUnresolvedEntity Walk(Identifier node)
		{
			throw new NotImplementedException ();
		}

		public IUnresolvedEntity Walk(IndexerExpression node)
		{
			throw new NotImplementedException ();
		}

		public IUnresolvedEntity Walk(InvocationExpression node)
		{
			throw new NotImplementedException ();
		}

		public IUnresolvedEntity Walk(LiteralExpression node)
		{
			throw new NotImplementedException ();
		}

		public IUnresolvedEntity Walk(MemberReferenceExpression node)
		{
			throw new NotImplementedException ();
		}

		public IUnresolvedEntity Walk(Statement node)
		{
			throw new NotImplementedException ();
		}

		public IUnresolvedEntity Walk(TimeFormatLiteral node)
		{
			throw new NotImplementedException ();
		}*/
		#endregion
	}
}

