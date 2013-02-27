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
		public Identifier Lhs{
			get{
				return (FirstChild.Type == NodeType.Identifier) ? (Identifier)FirstChild : null;
			}
		}
		
		public Expression Rhs{
			get{
				if(FirstChild != null && FirstChild.NextSibling != null)
					return (Expression)FirstChild.NextSibling;
				else
					return null;
			}
		}
		
		public override NodeType Type {
			get {
				return NodeType.Definition;
			}
		}
		
		public DefinitionExpression(Identifier left, Expression right, TextLocation start, TextLocation end) : base(start, end)
		{
			AddChild(left);
			AddChild(right);
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
		
		public override string GetText()
		{
			return "<Definition: " + Lhs.GetText() + " = " + Rhs.GetText();
		}
	}
}
