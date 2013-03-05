/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/03/02
 * Time: 14:35
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using ICSharpCode.NRefactory;

namespace BVE5Language.Ast
{
	/// <summary>
	/// Represents an unary expression.
	/// </summary>
	public class UnaryExpression : Expression
	{
		readonly Operator ope;
		
		public Operator Ope{
			get{return ope;}
		}
		
		public Expression Operand{
			get{return (Expression)FirstChild;}
		}
		
		public UnaryExpression(Expression operand, Operator ope, TextLocation start, TextLocation end)
			: base(start, end)
		{
			AddChild(operand);
			this.ope = ope;
		}
		
		public override NodeType Type {
			get {
				return NodeType.UnaryExpression;
			}
		}
		
		public override string GetText()
		{
			return BinaryExpression.GetOperatorString(ope) + Operand.GetText();
		}
		
		public override TResult AcceptWalker<TResult>(IAstWalker<TResult> walker)
		{
			return walker.Walk(this);
		}
		
		public override void AcceptWalker(AstWalker walker)
		{
			if(walker.Walk(this))
				Operand.AcceptWalker(walker);
			
			walker.PostWalk(this);
		}
	}
}
