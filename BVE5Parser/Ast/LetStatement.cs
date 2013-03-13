/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/03/11
 * Time: 0:48
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using ICSharpCode.NRefactory;

namespace BVE5Language.Ast
{
	/// <summary>
	/// Represents a let statement in route files like "let a = 1;".
	/// </summary>
	public class LetStatement : Statement
	{
		public LetStatement(Expression expr, TextLocation start, TextLocation end)
			: base(expr, start, end)
		{
		}
		
		public override void AcceptWalker(AstWalker walker)
		{
			base.AcceptWalker(walker);
		}
		
		public override TResult AcceptWalker<TResult>(IAstWalker<TResult> walker)
		{
			return walker.Walk(this);
		}
		
		public override string GetText()
		{
			return "let " + Expr.GetText() + ";";
		}
	}
}
