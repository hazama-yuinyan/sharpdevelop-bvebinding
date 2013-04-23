/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/03/13
 * Time: 16:01
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BVE5Language.Ast;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Editor.CodeCompletion;

namespace BVE5Binding.Completion
{
	/// <summary>
	/// Completion item provider for user defined names.
	/// </summary>
	public class UserDefinedNameCompletionItemProvider
	{
		string type_name;
		static readonly Regex position_stmt_finder = new Regex(@"^\d+;$", RegexOptions.Compiled);
		
		public UserDefinedNameCompletionItemProvider(string typeName)
		{
			type_name = typeName;
		}
		
		static string GetHeaderSection(IDocument doc)
		{
			var sb = new StringBuilder();
			for(int i = 1; i <= doc.TotalNumberOfLines; ++i){
				var line = doc.GetLine(i);
				if(position_stmt_finder.IsMatch(line.Text))
					break;
				
				sb.Append(line.Text);
				sb.AppendLine();
			}
			
			return sb.ToString();
		}
		
		public ICompletionItemList Provide(ITextEditor editor)
		{
			switch(type_name){
			case "Track":
			case "Repeater":
				var collector = new UserDefinedNameCollector(type_name);
				return ProvideImpl(editor, collector);
				
			case "Train":
				var fetcher = new NameFetcherForTrainType();
				return ProvideImpl(editor, fetcher);
				
			default:
				return ProvideImpl(editor);
			}
		}
		
		#region Implementation details
		ICompletionItemList ProvideImpl(ITextEditor editor, UserDefinedNameCollector collector)
		{
			var tree = ParserFactory.CreateRouteParser().Parse(editor.Document.Text, editor.FileName, true);
			var names = tree.AcceptWalker(collector).ToList();
			var list = CompletionDataHelper.GenerateCompletionList(names);
			return list;
		}
		
		ICompletionItemList ProvideImpl(ITextEditor editor, NameFetcherForTrainType fetcher)
		{
			var header_section = GetHeaderSection(editor.Document);
			var header_tree = ParserFactory.CreateRouteParser().Parse(header_section, editor.FileName, true);
			if(header_tree.Errors.Any())
				return null;
			
			var names = header_tree.AcceptWalker(fetcher).ToList();
			var list = CompletionDataHelper.GenerateCompletionList(names);
			return list;
		}
		
		ICompletionItemList ProvideImpl(ITextEditor editor)
		{
			var header_section = GetHeaderSection(editor.Document);
			var header_tree = ParserFactory.CreateRouteParser().Parse(header_section, editor.FileName, true);
			if(header_tree.Errors.Any())
				return null;
			
			var load_stmt_finder = new LoadStatementFinder(type_name);
			header_tree.AcceptWalker(load_stmt_finder);
			var list = CompletionDataHelper.GenerateCompletionList(load_stmt_finder.Names.ToList());
			return list;
		}
		
		/// <summary>
		/// A user defined name collector that collect from indexer expressions
		/// </summary>
		class UserDefinedNameCollector : IAstWalker<IEnumerable<string>>
		{
			readonly string context_name;
			
			internal UserDefinedNameCollector(string contextName)
			{
				context_name = contextName;
			}
			
			public IEnumerable<string> Walk(BinaryExpression binary)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(DefinitionExpression def)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(Identifier ident)
			{
				yield break;
			}
			
			public IEnumerable<string> Walk(IndexerExpression indexer)
			{
				var type_ident = indexer.Target as Identifier;
				if(type_ident != null && type_ident.Name == context_name)
					yield return (string)indexer.Index.Value.ToString();
				else
					yield break;
			}
			
			public IEnumerable<string> Walk(InvocationExpression invocation)
			{
				return invocation.Target.AcceptWalker(this);
			}
			
			public IEnumerable<string> Walk(LetStatement letStmt)
			{
				yield break;
			}
			
			public IEnumerable<string> Walk(LiteralExpression literal)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(MemberReferenceExpression memRef)
			{
				return memRef.Target.AcceptWalker(this);
			}
			
			public IEnumerable<string> Walk(SectionStatement secStmt)
			{
				yield break;
			}
			
			public IEnumerable<string> Walk(SequenceExpression sequence)
			{
				yield break;
			}
			
			public IEnumerable<string> Walk(Statement stmt)
			{
				if(stmt.Expr is InvocationExpression)
					return stmt.Expr.AcceptWalker(this);
				
				return Enumerable.Empty<string>();
			}
			
			public IEnumerable<string> Walk(SyntaxTree tree)
			{
				var names = new List<string>();
				foreach(var stmt in tree.Body)
					names.AddRange(stmt.AcceptWalker(this));
				
				return names.Distinct();
			}
			
			public IEnumerable<string> Walk(TimeFormatLiteral timeLiteral)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(UnaryExpression unary)
			{
				throw new NotImplementedException();
			}
		}
		
		/// <summary>
		/// A user defined name fetcher that collect names from Train.Add commands.
		/// </summary>
		class NameFetcherForTrainType : IAstWalker<IEnumerable<string>>
		{
			public IEnumerable<string> Walk(BinaryExpression binary)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(DefinitionExpression def)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(Identifier ident)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(IndexerExpression indexer)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(InvocationExpression invocation)
			{
				var mem_ref = invocation.Target as MemberReferenceExpression;
				if(mem_ref != null){
					var type_ident = mem_ref.Target as Identifier;
					if(type_ident != null && type_ident.Name == "Train" && mem_ref.Reference.Name == "Add"){
						var key_literal = invocation.Arguments.First() as LiteralExpression;
						if(key_literal != null)
							yield return (string)key_literal.Value;
						else
							yield break;
					}
				}
				
				yield break;
			}
			
			public IEnumerable<string> Walk(LetStatement letStmt)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(LiteralExpression literal)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(MemberReferenceExpression memRef)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(SectionStatement secStmt)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(SequenceExpression sequence)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(Statement stmt)
			{
				if(stmt.Expr is InvocationExpression)
					return stmt.Expr.AcceptWalker(this);
				
				return Enumerable.Empty<string>();
			}
			
			public IEnumerable<string> Walk(SyntaxTree tree)
			{
				var names = new List<string>();
				foreach(var stmt in tree.Body)
					names.AddRange(stmt.AcceptWalker(this));
				
				return names.Distinct();
			}
			
			public IEnumerable<string> Walk(TimeFormatLiteral timeLiteral)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(UnaryExpression unary)
			{
				throw new NotImplementedException();
			}
		}
		
		class LoadStatementFinder : AstWalker
		{
			readonly string type_name;
			string file_name, directory_name;
			
			public IEnumerable<string> Names{
				get; private set;
			}
			
			internal LoadStatementFinder(string typeName)
			{
				type_name = typeName;
			}
			
			static bool IsValidPath(string path)
			{
				return path != null && Path.HasExtension(path);
			}
			
			public override bool Walk(DefinitionExpression def)
			{
				return base.Walk(def);
			}
			
			public override bool Walk(InvocationExpression invocation)
			{
				var mem_ref = invocation.Target as MemberReferenceExpression;
				if(mem_ref != null){
					var type_ident = mem_ref.Target as Identifier;
					if(mem_ref.Reference.Name == "Load" && type_ident != null && type_ident.Name == type_name){
						if(LoggingService.IsDebugEnabled)
							LoggingService.DebugFormatted("A Load command found: {0}.Load", type_ident.Name);
						
						invocation.Arguments.First().AcceptWalker(this);
						var parser = ParserFactory.CreateCommonParser(BVE5LanguageBinding.GetFileKindFromTypeName(type_name));
						var target_file_path = Path.Combine(directory_name, file_name);
						var tree = parser.Parse(target_file_path);
						var fetcher = new UserDefinedNameFetcher();
						Names = tree.AcceptWalker(fetcher);
					}
				}
				
				return false;
			}
			
			public override bool Walk(SyntaxTree unit)
			{
				directory_name = Path.GetDirectoryName(unit.Name);
				return true;
			}
			
			public override bool Walk(LiteralExpression literal)
			{
				if(IsValidPath(literal.Value as string)){
					if(LoggingService.IsDebugEnabled)
						LoggingService.DebugFormatted("A file path found: {0}", literal.Value);
					
					file_name = (string)literal.Value;
				}
					
				return false;
			}
		}
		
		class UserDefinedNameFetcher : IAstWalker<IEnumerable<string>>
		{
			public IEnumerable<string> Walk(BinaryExpression binary)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(DefinitionExpression def)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(Identifier ident)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(IndexerExpression indexer)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(InvocationExpression invocation)
			{
				var key_literal = invocation.Arguments.First() as LiteralExpression;
				if(key_literal != null)
					yield return (string)key_literal.Value;
				else
					yield break;
			}
			
			public IEnumerable<string> Walk(LetStatement letStmt)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(LiteralExpression literal)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(MemberReferenceExpression memRef)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(SectionStatement secStmt)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(SequenceExpression sequence)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(Statement stmt)
			{
				if(stmt.Expr is InvocationExpression)
					return stmt.Expr.AcceptWalker(this);
				
				return Enumerable.Empty<string>();
			}
			
			public IEnumerable<string> Walk(SyntaxTree tree)
			{
				var names = new List<string>();
				foreach(var stmt in tree.Body)
					names.AddRange(stmt.AcceptWalker(this));
				
				return names;
			}
			
			public IEnumerable<string> Walk(TimeFormatLiteral timeLiteral)
			{
				throw new NotImplementedException();
			}
			
			public IEnumerable<string> Walk(UnaryExpression unary)
			{
				throw new NotImplementedException();
			}
		}
		#endregion
	}
}
