/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/02/26
 * Time: 14:24
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Text;
using ICSharpCode.NRefactory;

namespace BVE5Language.Ast
{
	/// <summary>
	/// Represents a comma-separated list such as "1,1:00:30,child\a.txt".
	/// </summary>
	public class SequenceExpression : Expression
	{
		private readonly Expression[] exprs;
		
		public Expression[] Expressions{
			get{return exprs;}
		}
		
		public SequenceExpression(Expression[] expressions, TextLocation start, TextLocation end) : base(start, end)
		{
			exprs = expressions;
		}
		
		public override NodeType Type {
			get {
				return NodeType.Sequence;
			}
		}
		
		public override string GetText()
		{
			var sb = new StringBuilder();
			foreach(var expr in exprs){
				sb.Append(expr.GetText());
				sb.Append(",");
			}
			sb.Remove(sb.Length - 1, 1);		//remove the trailing comma
			return sb.ToString();
		}
		
		public override void AcceptWalker(AstWalker walker)
		{
			if(walker.Walk(this)){
				foreach(var child in exprs)
					child.AcceptWalker(walker);
			}
			walker.PostWalk(this);
		}
		
		public override TResult AcceptWalker<TResult>(IAstWalker<TResult> walker)
		{
			return walker.Walk(this);
		}
	}
}
