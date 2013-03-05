/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/03/02
 * Time: 14:33
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using ICSharpCode.NRefactory;
using BVE5Language.Parser;

namespace BVE5Language.Ast
{
	public enum Operator
	{
		Plus,
		Minus,
		Multiply,
		Divide
	}
	
	/// <summary>
	/// Represents a binary expression.
	/// </summary>
	public class BinaryExpression : Expression
	{
		readonly Operator ope;
		
		public Operator Ope{
			get{return ope;}
		}
		
		public Expression Lhs{
			get{
				return (Expression)FirstChild;
			}
		}
		
		public Expression Rhs{
			get{
				return (Expression)FirstChild.NextSibling;
			}
		}
		
		public BinaryExpression(Expression lhs, Expression rhs, Operator ope, TextLocation start, TextLocation end)
			: base(start, end)
		{
			AddChild(lhs);
			AddChild(rhs);
			this.ope = ope;
		}
		
		public override NodeType Type {
			get {
				return NodeType.BinaryExpression;
			}
		}
		
		internal static string GetOperatorString(Operator ope)
		{
			switch(ope){
			case Operator.Plus:
				return "+";
				
			case Operator.Minus:
				return "-";
				
			case Operator.Multiply:
				return "*";
				
			case Operator.Divide:
				return "/";
				
			default:
				throw new BVE5ParserException("Unknown operator type!");
			}
		}
		
		public override string GetText()
		{
			return Lhs.GetText() + GetOperatorString(ope) + Rhs.GetText();
		}
		
		public override TResult AcceptWalker<TResult>(IAstWalker<TResult> walker)
		{
			return walker.Walk(this);
		}
		
		public override void AcceptWalker(AstWalker walker)
		{
			if(walker.Walk(this)){
				Lhs.AcceptWalker(walker);
				Rhs.AcceptWalker(walker);
			}
			walker.PostWalk(this);
		}
	}
}
