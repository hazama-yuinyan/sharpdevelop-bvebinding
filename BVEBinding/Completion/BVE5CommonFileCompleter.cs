/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/03/09
 * Time: 15:08
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BVE5Language;
using BVE5Language.Ast;
using BVE5Language.Parser;
using BVE5Language.Resolver;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Editor.CodeCompletion;

namespace BVE5Binding.Completion
{
	/// <summary>
	/// Code completer for BVE5 common files.
	/// </summary>
	public class BVE5CommonFileCompleter : ICodeCompleter
	{
		readonly CommonFileCommandInfo SemanticInfo;
		readonly BVE5FileKind kind;
		BVE5CommonParser parser;
		
		internal BVE5CommonFileCompleter(BVE5FileKind kind)
		{
			SemanticInfo = BVE5ResourceManager.CommonFileSemanticInfos[kind.ToString()];
			parser = ParserFactory.CreateCommonParser(kind);
			this.kind = kind;
		}
		
		public Tuple<bool, CodeCompletionKeyPressResult> TryComplete(ITextEditor editor, char ch, IInsightWindowHandler insightWindowHandler)
		{
			int cursor_offset = editor.Caret.Offset;
			if(char.IsLetterOrDigit(ch) && CodeCompletionOptions.InsightEnabled){
				var insight_window = editor.ShowInsightWindow(new []{ProvideInsight(editor)});
				if(insight_window != null && insightWindowHandler != null){
					insightWindowHandler.InitializeOpenedInsightWindow(editor, insight_window);
					insightWindowHandler.HighlightParameter(insight_window, 0);
				}
				return Tuple.Create(true, CodeCompletionKeyPressResult.Completed);
			}
			
			return Tuple.Create(false, CodeCompletionKeyPressResult.None);
		}
		
		internal IInsightItem ProvideInsight(ITextEditor editor)
		{
			return new CommandInsightItem(CreateHeaderText(kind.ToString(), SemanticInfo.Args), BVE5ResourceManager.GetDocumentationString(SemanticInfo.Doc));
		}
		
		static string CreateHeaderText(string commandName, ArgumentAnnotation[] args)
		{
			var sb = new StringBuilder(commandName);
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
