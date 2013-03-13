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
		
		public Tuple<bool, CodeCompletionKeyPressResult> TryComplete(ITextEditor editor, char ch, IInsightWindowHandler insightWindowHandler)
		{
			int cursor_offset = editor.Caret.Offset;
			if(ch == '['){
				var line = editor.Document.GetLineForOffset(cursor_offset);
				current_context_type = line.Text.Trim();
			}else if(ch == ',' && CodeCompletionOptions.InsightRefreshOnComma && CodeCompletionOptions.InsightEnabled){
				IInsightWindow insight_window;
				if(insightWindowHandler.InsightRefreshOnComma(editor, ch, out insight_window))
					return Tuple.Create(true, CodeCompletionKeyPressResult.Completed);
			}else if(ch == '.'){
				var line = editor.Document.GetLineForOffset(cursor_offset);
				var type_name = (current_context_type != null) ? current_context_type : line.Text.Trim();
				var builtin_asm = BVEBuiltins.GetBuiltinAssembly();
				var result = CodeCompletionKeyPressResult.None;
				
				foreach(var type_def in builtin_asm.TopLevelTypeDefinitions){
					if(type_def.Name == type_name){
						var names = type_def.Members
							.Where(member => member.Name != "indexer")
							.Select(m => m.Name);
						var list = CompletionDataHelper.CreateListFromString(names);
						editor.ShowCompletionWindow(list);
						result = CodeCompletionKeyPressResult.Completed;
						break;
					}
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
				var list = CompletionDataHelper.CreateListFromString(builtin_type_names);
				editor.ShowCompletionWindow(list);
				return Tuple.Create(true, CodeCompletionKeyPressResult.CompletedIncludeKeyInCompletion);
			}
			
			return Tuple.Create(false, CodeCompletionKeyPressResult.None);
		}
		
		internal IInsightItem[] ProvideInsight(ITextEditor editor)
		{
			var line = editor.Document.GetLineForOffset(editor.Caret.Offset);
			var snippets = line.Text.Trim().Split('.');
			var type_name = snippets[0].Split('[');
			var annots = BVE5ResourceManager.GetRouteFileMemberAnnotation(type_name[0], snippets[1]);
			
			var res = new List<IInsightItem>();
			foreach(var annot in annots){
				var item = new CommandInsightItem(CreateHeaderText(type_name[0], snippets[1], annot.Args), BVE5ResourceManager.GetDocumentationString(annot.Doc));
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
	}
}
