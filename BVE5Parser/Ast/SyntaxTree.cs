//
// SyntaxTree.cs
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
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.TypeSystem;
using BVE5Language.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace BVE5Language.Ast
{
	/// <summary>
	/// Represents the syntax tree of a BVE5 route file.
	/// </summary>
	public class SyntaxTree : AstNode
	{
		readonly string name, version;
		readonly BVE5FileKind kind;
		List<Error> errors;
		
		public List<Error> Errors{
			get{return errors;}
		}

		public IEnumerable<Statement> Body{
			get{
				for(var node = FirstChild; node != null; node = node.NextSibling)
					yield return (Statement)node;
			}
		}

		public string Name{
			get{return name;}
		}
		
		public string Version{
			get{return version;}
		}
		
		public BVE5FileKind Kind{
			get{return kind;}
		}

		public override NodeType Type {
			get {
				return NodeType.Tree;
			}
		}

		public SyntaxTree(List<Statement> body, string treeName, string versionString, BVE5FileKind fileKind, TextLocation startLoc, TextLocation endLoc, List<Error> errors)
			: base(startLoc, endLoc)
		{
			foreach(var stmt in body)
				AddChild(stmt);
			
			name = treeName;
			version = versionString;
			kind = fileKind;
			this.errors = errors;
		}

		public override void AcceptWalker(AstWalker walker)
		{
			if(walker.Walk(this)){
				foreach(var stmt in Body)
					stmt.AcceptWalker(walker);
			}
			walker.PostWalk(this);
		}

		public override TResult AcceptWalker<TResult>(IAstWalker<TResult> walker)
		{
			return walker.Walk(this);
		}

		public override string GetText()
		{
			return "<SyntaxTree: " + kind.ToString() + ">";
		}

		/// <summary>
		/// Converts this syntax tree into a parsed file that can be stored in the type system.
		/// </summary>
		public BVE5UnresolvedFile ToTypeSystem()
		{
			if(string.IsNullOrEmpty(name))
				throw new InvalidOperationException("Cannot use ToTypeSystem() on a syntax tree without file name.");

			var type_def = new DefaultUnresolvedTypeDefinition("global", FileKindHelper.GetTypeNameFromFileKind(kind));
			var walker = new TypeSystemConvertWalker(new BVE5UnresolvedFile(name, type_def, null));
			walker.Walk(this);
			return walker.UnresolvedFile;
		}
	}
}

