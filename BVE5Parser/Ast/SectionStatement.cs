/*
 * Created by SharpDevelop.
 * User: Ryouta
 * Date: 2013/02/26
 * Time: 1:59
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using ICSharpCode.NRefactory;

namespace BVE5Language.Ast
{
	/// <summary>
	/// Description of SectionStatement.
	/// </summary>
	public class SectionStatement : Statement
	{
		private readonly Identifier ident;
		
		public Identifier Name{
			get{return ident;}
		}
		
		public SectionStatement(Identifier sectionName, TextLocation start, TextLocation end) : base(sectionName, start, end)
		{
			ident = sectionName;
		}
		
		public override NodeType Type {
			get { return NodeType.SectionStmt; }
		}
		
		public override void AcceptWalker(AstWalker walker)
		{
			if(walker.Walk(this)){}
			walker.PostWalk(this);
		}
		
		public override TResult AcceptWalker<TResult>(IAstWalker<TResult> walker)
		{
			return walker.Walk(this);
		}
	}
}
