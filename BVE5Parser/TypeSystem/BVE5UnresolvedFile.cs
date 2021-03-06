//
// BVE5UnresolvedFile.cs
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
using System.Linq;

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.TypeSystem;

namespace BVE5Language.TypeSystem
{
	public class BVE5UnresolvedFile : IUnresolvedFile
	{
		readonly string file_name;
		readonly List<IUnresolvedTypeDefinition> toplevel_type_defs;
		List<Error> errors;

		public BVE5UnresolvedFile(string fileName)
		{
			file_name = fileName;
		}

		public BVE5UnresolvedFile(string fileName, IUnresolvedTypeDefinition unresolvedTypeDef, List<Error> errorList)
		{
			file_name = fileName;
			toplevel_type_defs = new List<IUnresolvedTypeDefinition>{unresolvedTypeDef};
			errors = errorList;
		}

		#region IUnresolvedFile implementation
		public IUnresolvedTypeDefinition GetTopLevelTypeDefinition(TextLocation location)
		{
			return toplevel_type_defs[0];
		}

		public IUnresolvedTypeDefinition GetInnermostTypeDefinition(TextLocation location)
		{
			return toplevel_type_defs[0];
		}

		public IUnresolvedMember GetMember(TextLocation location)
		{
			IUnresolvedTypeDefinition type = GetInnermostTypeDefinition(location);
			if(type == null)
				return null;
			
			return FindEntity(type.Members, location);
		}
		
		static T FindEntity<T>(IList<T> list, TextLocation location) where T : class, IUnresolvedEntity
		{
			// This could be improved using a binary search
			foreach(T entity in list){
				if(entity.Region.IsInside(location.Line, location.Column))
					return entity;
			}
			return null;
		}

		public ITypeResolveContext GetTypeResolveContext(ICompilation compilation, TextLocation loc)
		{
			ITypeResolveContext ctx = new SimpleTypeResolveContext(compilation.MainAssembly);
			var cur_def = GetInnermostTypeDefinition(loc);
			if(cur_def != null){
				var resolved_def = cur_def.Resolve(ctx).GetDefinition();
				if(resolved_def == null)
					return ctx;
				
				ctx = ctx.WithCurrentTypeDefinition(resolved_def);
				
				var cur_member = resolved_def.Members.FirstOrDefault(m => m.Region.FileName == FileName && m.Region.Begin <= loc && loc < m.BodyRegion.End);
				if(cur_member != null)
					ctx = ctx.WithCurrentMember(cur_member);
			}
			
			return ctx;
		}

		public string FileName {
			get {
				return file_name;
			}
		}

		public DateTime? LastWriteTime{get; set;}

		public IList<IUnresolvedTypeDefinition> TopLevelTypeDefinitions {
			get {
				return toplevel_type_defs;
			}
		}

		public IList<IUnresolvedAttribute> AssemblyAttributes {
			get {
				throw new NotImplementedException("Never implemented!");
			}
		}

		public IList<IUnresolvedAttribute> ModuleAttributes {
			get {
				throw new NotImplementedException("Never implemented!");
			}
		}

		public IList<Error> Errors {
			get {
				return errors;
			}
			internal set{
				errors = (List<Error>)value;
			}
		}
		#endregion
	}
}

