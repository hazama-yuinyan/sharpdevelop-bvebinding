/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/03/08
 * Time: 13:28
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BVE5Language.Ast;
using BVE5Language.Parser;
using BVE5Language.Resolver;
using BVE5Language.TypeSystem;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Editor.CodeCompletion;

namespace BVE5Binding.Completion
{
	/// <summary>
	/// Insight window handler for BVE5 route file code.
	/// </summary>
	public class BVE5RouteFileInsightWindowHandler : IInsightWindowHandler
	{
		int highlighted_parameter;
		BVE5RouteFileCompleter completer;
		
		public BVE5RouteFileInsightWindowHandler(BVE5RouteFileCompleter completer)
		{
			this.completer = completer;
		}
		
		public void InitializeOpenedInsightWindow(ITextEditor editor, IInsightWindow insightWindow)
		{
			EventHandler<TextChangeEventArgs> on_document_changed = delegate{
				// whenever the document is changed, recalculate EndOffset
				var remaining_doc = editor.Document.CreateReader(insightWindow.StartOffset, editor.Document.TextLength - insightWindow.StartOffset);
				var lexer = new BVE5RouteFileLexer(remaining_doc);
				var line = editor.Document.GetLineForOffset(insightWindow.StartOffset);
				lexer.SetInitialLocation(line.LineNumber, insightWindow.StartOffset - editor.Document.PositionToOffset(line.LineNumber, 1));
				Token token;
				
				lexer.Advance();
				while((token = lexer.Current) != null && token.Kind != TokenKind.EOF){
					if(token.Literal == ")"){
						MarkInsightWindowEndOffset(insightWindow, editor, token.StartLoc);
						break;
					}else if(token.Literal == ";"){
						MarkInsightWindowEndOffset(insightWindow, editor, token.StartLoc);
						break;
					}
					lexer.Advance();
				}
			};
			
			insightWindow.DocumentChanged += on_document_changed;
			insightWindow.SelectedItemChanged += delegate { HighlightParameter(insightWindow, highlighted_parameter); };
			on_document_changed(null, null);
		}

		void MarkInsightWindowEndOffset(IInsightWindow insightWindow, ITextEditor editor, TextLocation endLocation)
		{
			insightWindow.EndOffset = editor.Document.PositionToOffset(endLocation.Line, endLocation.Column);
			if(editor.Caret.Offset > insightWindow.EndOffset)
				insightWindow.Close();
		}
		
		IList<ResolveResult> ResolveCallArguments(ITextEditor editor)
		{
			var rr = new List<ResolveResult>();
			int cursor_offset = editor.Caret.Offset;
			var line_text = editor.Document.GetLineForOffset(cursor_offset).Text;
			var parser = ParserFactory.CreateRouteParser();
			var stmt = parser.ParseOneStatement(line_text) as Statement;
			if(stmt != null){
				var command_invoke = stmt.Expr as InvocationExpression;
				if(command_invoke == null)
					return rr;
				
				var resolver = new BVE5AstResolver(new BVE5Resolver(BVE5LanguageBinding.Compilation), stmt,
				                                   (BVE5UnresolvedFile)BVE5LanguageBinding.ProjectContent.GetFile(editor.FileName));
				foreach(var arg in command_invoke.Arguments)
					rr.Add(resolver.Resolve(arg));
			}
			
			return rr;
		}
		
		public bool InsightRefreshOnComma(ITextEditor editor, char ch, out IInsightWindow insightWindow)
		{
			int cursor_offset = editor.Caret.Offset;
			var line = editor.Document.GetLineForOffset(cursor_offset);
			if(line.Text.IndexOf('#') == -1){
				var insight_items = completer.ProvideInsight(editor);
				
				// find highlighted parameter
				// the number of recognized parameters is the index of the current parameter!
				var args = ResolveCallArguments(editor);
				highlighted_parameter = args.Count;
				insightWindow = ShowInsight(editor, insight_items, args);
				return insightWindow != null;
			}
			
			insightWindow = null;
			return false;
		}
		
		IInsightWindow ShowInsight(ITextEditor editor, IList<IInsightItem> insightItems, ICollection<ResolveResult> arguments)
		{
			if(insightItems == null || !insightItems.Any())
				return null;
			
			int default_index;
			if(insightItems.Count == 1){
				default_index = 0;
			}else{
				//var or = new OverloadResolution(BVE5LanguageBinding.Compilation, arguments.ToArray());
				//or
				return null;
			}
			
			var insight_window = editor.ShowInsightWindow(insightItems);
			if(insight_window != null){
				InitializeOpenedInsightWindow(editor, insight_window);
				insight_window.SelectedItem = insightItems[default_index];
			}
			
			return insight_window;
		}
		
		public void HighlightParameter(IInsightWindow window, int index)
		{
			if(window == null)
				return;
			
			var item = window.SelectedItem as CommandInsightItem;
			if(item != null)
				item.HighlightParameter = index;
			
			highlighted_parameter = index;
		}
	}
}
