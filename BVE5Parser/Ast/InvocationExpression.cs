//
// InvocationExpression.cs
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
using System.Collections.Generic;
using System.Text;
using ICSharpCode.NRefactory;

namespace BVE5Language.Ast
{
	/// <summary>
	/// Represents a command invoke expression.
	/// </summary>
	public class InvocationExpression : Expression
	{
		public Expression Target{
			get{return (Expression)FirstChild;}
		}

		public IEnumerable<Expression> Arguments{
			get{
				for(var node = FirstChild.NextSibling; node != null; node = node.NextSibling)
					yield return (Expression)node;
			}
		}

		public override NodeType Type {
			get {
				return NodeType.Invocation;
			}
		}

		public InvocationExpression(Expression target, List<Expression> arguments, TextLocation startLoc, TextLocation endLoc)
			: base(startLoc, endLoc)
		{
			AddChild(target);
			foreach(var arg in arguments)
				AddChild(arg);
		}

		public override void AcceptWalker(AstWalker walker)
		{
			if(walker.Walk(this)){
				Target.AcceptWalker(walker);
				foreach(var arg in Arguments)
					arg.AcceptWalker(walker);
			}
			walker.PostWalk(this);
		}

		public override TResult AcceptWalker<TResult>(IAstWalker<TResult> walker)
		{
			return walker.Walk(this);
		}

		public override string GetText()
		{
			var sb = new StringBuilder(Target.GetText() + "(");
			foreach(var arg in Arguments)
				sb.Append(arg.GetText());

			sb.Append(")");
			return sb.ToString();
		}
	}
}

