/*
 * Created by SharpDevelop.
 * User: HAZAMA
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
	/// Represents a section statement in init files like "[section]".
	/// </summary>
	public class SectionStatement : Statement
	{
		public Identifier SectionName{
			get{return (Identifier)FirstChild;}
		}
		
		public SectionStatement(Identifier sectionName, TextLocation start, TextLocation end) : base(sectionName, start, end)
		{
			AddChild(sectionName);
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
		
		public override string GetText()
		{
			return "[Section: " + SectionName.GetText() + "]";
		}
	}
}
