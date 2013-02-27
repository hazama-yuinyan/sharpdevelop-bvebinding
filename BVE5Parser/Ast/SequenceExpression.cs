/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/02/26
 * Time: 14:24
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.NRefactory;

namespace BVE5Language.Ast
{
	/// <summary>
	/// Represents a comma-separated list such as "1,1:00:30,child\a.txt".
	/// </summary>
	public class SequenceExpression : Expression
	{
		public IEnumerable<Expression> Expressions{
			get{
				for(var node = FirstChild; node != null; node = node.NextSibling)
					yield return (Expression)node;
			}
		}
		
		public SequenceExpression(List<Expression> expressions, TextLocation start, TextLocation end) : base(start, end)
		{
			foreach(var expr in expressions)
				AddChild(expr);
		}
		
		public override NodeType Type {
			get {
				return NodeType.Sequence;
			}
		}
		
		public override void AcceptWalker(AstWalker walker)
		{
			if(walker.Walk(this)){
				foreach(var child in Expressions)
					child.AcceptWalker(walker);
			}
			walker.PostWalk(this);
		}
		
		public override TResult AcceptWalker<TResult>(IAstWalker<TResult> walker)
		{
			return walker.Walk(this);
		}
		
		public override string GetText()
		{
			var sb = new StringBuilder();
			foreach(var expr in Expressions){
				sb.Append(expr.GetText());
				sb.Append(",");
			}
			sb.Remove(sb.Length - 1, 1);		//remove the trailing comma
			return sb.ToString();
		}
	}
}
