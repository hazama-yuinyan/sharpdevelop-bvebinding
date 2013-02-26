/*
 * Created by SharpDevelop.
 * User: Ryouta
 * Date: 02/22/2013
 * Time: 16:22
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using ICSharpCode.NRefactory;

namespace BVE5Language.Ast
{
	/// <summary>
	/// Represents a definition such as "name = 1". Used in ini files.
	/// </summary>
	/// <remarks>
	/// In programming languages, the equal sign usually denotes assignment, but in BVE5 it instead denotes definition or equality as in a mathematical expression.
	/// </remarks>
	public class DefinitionExpression : Expression
	{
		private readonly Identifier lhs;
		private readonly Expression rhs;
		
		public Identifier Lhs{
			get{return lhs;}
		}
		
		public Expression Rhs{
			get{return rhs;}
		}
		
		public override NodeType Type {
			get {
				return NodeType.Definition;
			}
		}
		
		public DefinitionExpression(Identifier left, Expression right, TextLocation start, TextLocation end) : base(start, end)
		{
			lhs = left;
			rhs = right;
		}
		
		public override string GetText()
		{
			return "<Definition: " + lhs.GetText() + " = " + rhs.GetText();
		}
		
		public override TResult AcceptWalker<TResult>(IAstWalker<TResult> walker)
		{
			return walker.Walk(this);
		}
		
		public override void AcceptWalker(AstWalker walker)
		{
			if(walker.Walk(this)){
				lhs.AcceptWalker(walker);
				rhs.AcceptWalker(walker);
			}
			walker.PostWalk(this);
		}
	}
}
