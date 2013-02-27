using System;

using ICSharpCode.NRefactory;

namespace BVE5Language.Ast
{
	/// <summary>
	/// Represents a statement.
	/// </summary>
	public class Statement : AstNode
	{
		public Expression Expr{
			get{return (Expression)FirstChild;}
		}

		public override NodeType Type {
			get {
				return NodeType.Statement;
			}
		}

		public Statement(Expression expr, TextLocation startLoc, TextLocation endLoc)
			: base(startLoc, endLoc)
		{
			AddChild(expr);
		}

		public override void AcceptWalker(AstWalker walker)
		{
			if(walker.Walk(this)){
				if(Expr != null)
					Expr.AcceptWalker(walker);
			}

			walker.PostWalk(this);
		}

		public override TResult AcceptWalker<TResult>(IAstWalker<TResult> walker)
		{
			return walker.Walk(this);
		}

		public override string GetText()
		{
			return Expr.GetText() + ";";
		}
	}
}

