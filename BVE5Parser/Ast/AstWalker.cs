//
// AstWalker.cs
//
// Author:
//       HAZAMA <kotonechan@live.jp>
//
// Copyright (c) 2013 HAZAMA
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;

namespace BVE5Language.Ast
{
	/// <summary>
	/// An ast walker.(default return value is true)
	/// </summary>
	public class AstWalker
	{
		//protected virtual bool Walk(AstNode node){return true;}
		
		public virtual bool Walk(DefinitionExpression def){return true;}
		public virtual void PostWalk(DefinitionExpression def){}

		public virtual bool Walk(Expression expr){return true;}
		public virtual void PostWalk(Expression expr){}

		public virtual bool Walk(Identifier ident){return true;}
		public virtual void PostWalk(Identifier ident){}

		public virtual bool Walk(IndexerExpression indexingExpr){return true;}
		public virtual void PostWalk(IndexerExpression indexingExpr){}

		public virtual bool Walk(InvocationExpression invocation){return true;}
		public virtual void PostWalk(InvocationExpression invocation){}

		public virtual bool Walk(LiteralExpression literal){return true;}
		public virtual void PostWalk(LiteralExpression literal){}

		public virtual bool Walk(MemberReferenceExpression memRef){return true;}
		public virtual void PostWalk(MemberReferenceExpression memRef){}
		
		public virtual bool Walk(SectionStatement secStmt){return true;}
		public virtual void PostWalk(SectionStatement secStmt){}
		
		public virtual bool Walk(SequenceExpression sequence){return true;}
		public virtual void PostWalk(SequenceExpression sequence){}

		public virtual bool Walk(Statement stmt){return true;}
		public virtual void PostWalk(Statement stmt){}

		public virtual bool Walk(SyntaxTree unit){return true;}
		public virtual void PostWalk(SyntaxTree unit){}

		public virtual bool Walk(TimeFormatLiteral timeLiteral){return true;}
		public virtual void PostWalk(TimeFormatLiteral timeLiteral){}
	}

	public interface IAstWalker<TResult>
	{
		TResult Walk(DefinitionExpression def);
		TResult Walk(Identifier node);
		TResult Walk(IndexerExpression node);
		TResult Walk(InvocationExpression node);
		TResult Walk(LiteralExpression node);
		TResult Walk(MemberReferenceExpression node);
		TResult Walk(SectionStatement secStmt);
		TResult Walk(SequenceExpression sequence);
		TResult Walk(Statement node);
		TResult Walk(SyntaxTree node);
		TResult Walk(TimeFormatLiteral node);
	}
}

