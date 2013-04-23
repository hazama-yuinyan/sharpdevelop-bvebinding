/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/03/08
 * Time: 14:44
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BVE5Language.Ast;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Editor.CodeCompletion;
using BVE5Language;
using BVE5Language.TypeSystem;

namespace BVE5Binding.Completion
{
	/// <summary>
	/// Code Completer for BVE5 route file.
	/// </summary>
	public class BVE5RouteFileCompleter : ICodeCompleter
	{
		string current_context_type = null;
		static readonly Regex CodeSnippetFinder = new Regex(@"([a-zA-Z]+)(?:\[.+?\])?\.([a-zA-Z0-9]+)", RegexOptions.Compiled);
		
		public Tuple<bool, CodeCompletionKeyPressResult> TryComplete(ITextEditor editor, char ch, IInsightWindowHandler insightWindowHandler)
		{
			int cursor_offset = editor.Caret.Offset;
			if(ch == '['){
				var line = editor.Document.GetLineForOffset(cursor_offset);
				current_context_type = line.Text.Trim();
				var provider = new UserDefinedNameCompletionItemProvider(current_context_type);
				var list = provider.Provide(editor);
				if(list != null){
					editor.ShowCompletionWindow(list);
					return Tuple.Create(true, CodeCompletionKeyPressResult.Completed);
				}else{
					return Tuple.Create(false, CodeCompletionKeyPressResult.None);
				}
			}else if(ch == ',' && CodeCompletionOptions.InsightRefreshOnComma && CodeCompletionOptions.InsightEnabled){
				IInsightWindow insight_window;
				if(insightWindowHandler.InsightRefreshOnComma(editor, ch, out insight_window))
					return Tuple.Create(true, CodeCompletionKeyPressResult.Completed);
			}else if(ch == '.'){
				var line = editor.Document.GetLineForOffset(cursor_offset);
				var type_name = (current_context_type != null) ? current_context_type : line.Text.Trim();
				var semantic_infos = BVE5ResourceManager.RouteFileSemanticInfos;
				var result = CodeCompletionKeyPressResult.None;
				if(semantic_infos.ContainsKey(type_name)){
					var type_semantic_info = semantic_infos[type_name];
					
					var names = type_semantic_info
						.Where(member => member.Key != "indexer")
						.Select(m => m.Key)
						.Distinct()
						.ToList();
					var descriptions = type_semantic_info
						.Where(member => member.Key != "indexer")
						.Select(m => BVE5ResourceManager.GetDocumentationString(m.Value[0].Doc))
						.ToList();
					
					var list = CompletionDataHelper.GenerateCompletionList(names, descriptions);
					editor.ShowCompletionWindow(list);
					result = CodeCompletionKeyPressResult.Completed;
				}
				
				if(current_context_type != null) current_context_type = null;
				
				return Tuple.Create(result != CodeCompletionKeyPressResult.None, result);
			}else if(ch == '(' && CodeCompletionOptions.InsightEnabled){
				var insight_window = editor.ShowInsightWindow(ProvideInsight(editor));
				if(insight_window != null && insightWindowHandler != null){
					insightWindowHandler.InitializeOpenedInsightWindow(editor, insight_window);
					insightWindowHandler.HighlightParameter(insight_window, 0);
				}
				return Tuple.Create(true, CodeCompletionKeyPressResult.Completed);
			}
			
			if(char.IsLetter(ch) && CodeCompletionOptions.CompleteWhenTyping){
				var builtin_type_names = BVE5ResourceManager.GetAllTypeNames();
				var list = CompletionDataHelper.GenerateCompletionList(builtin_type_names);
				list = AddTemplateCompletionItems(editor, list, ch);
				editor.ShowCompletionWindow(list);
				return Tuple.Create(true, CodeCompletionKeyPressResult.CompletedIncludeKeyInCompletion);
			}
			
			return Tuple.Create(false, CodeCompletionKeyPressResult.None);
		}
		
		internal IInsightItem[] ProvideInsight(ITextEditor editor)
		{
			var line = editor.Document.GetLineForOffset(editor.Caret.Offset);
			var match = CodeSnippetFinder.Match(line.Text);
			var type_name = match.Groups[1].Value;
			var command_name = match.Groups[2].Value;
			var annots = BVE5ResourceManager.GetRouteFileMemberAnnotation(type_name, command_name);
			
			var res = new List<IInsightItem>();
			foreach(var annot in annots){
				var item = new CommandInsightItem(CreateHeaderText(type_name, command_name, annot.Args), BVE5ResourceManager.GetDocumentationString(annot.Doc));
				res.Add(item);
			}
			return res.ToArray();
		}
		
		static string CreateHeaderText(string typeName, string commandName, ArgumentAnnotation[] args)
		{
			var sb = new StringBuilder(typeName + "." + commandName);
			sb.Append('(');
			foreach(var arg in args){
				sb.Append(arg.ParamType);
				sb.Append(' ');
				sb.Append(arg.Name);
				sb.Append(", ");
			}
			sb.Replace(", ", ")", sb.Length - 2, 2);
			return sb.ToString();
		}
		
		static ICompletionItemList AddTemplateCompletionItems(ITextEditor editor, ICompletionItemList list, char ch)
		{
			if(list == null) return null;
			
			if(ch == 'c' || ch == 'C'){
				var res = new DefaultCompletionItemList();
				res.Items.AddRange(list.Items);
				res.Items.Add(new CurveTemplateCompletionItem(editor));
				return res;
			}
			
			return null;
		}
	}
}
