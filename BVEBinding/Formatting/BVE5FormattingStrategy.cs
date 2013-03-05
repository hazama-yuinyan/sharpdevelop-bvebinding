/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/02/13
 * Time: 15:28
 * 
 * For license, please see license.txt.
 */
using System;
using System.Text.RegularExpressions;
using ICSharpCode.SharpDevelop.Editor;

namespace BVEBinding.Formatting
{
	/// <summary>
	/// This class handles the auto and smart indenting in the textbuffer while
	/// you type.
	/// </summary>
	public class BVE5FormattingStrategy : DefaultFormattingStrategy
	{
		bool immediately_after_newline = false;
		static Regex pos_stmt_searcher = new Regex(@"^\s*\d+", RegexOptions.Compiled);
		
		#region DefaultFormattingStrategy members
		public override void FormatLine(ITextEditor editor, char charTyped)
		{
			editor.Document.StartUndoableAction();
			if(charTyped == '\n'){
				immediately_after_newline = true;
				TryIndent(editor, editor.Caret.Line, editor.Caret.Line);
			}else if(immediately_after_newline && char.IsDigit(charTyped)){
				TryIndent(editor, editor.Caret.Line, editor.Caret.Line);
				immediately_after_newline = false;
			}else if(immediately_after_newline){
				immediately_after_newline = false;
			}
			editor.Document.EndUndoableAction();
		}
		
		public override void IndentLine(ITextEditor editor, IDocumentLine line)
		{
			editor.Document.StartUndoableAction();
			TryIndent(editor, line.LineNumber, line.LineNumber);
			editor.Document.EndUndoableAction();
		}
		
		public override void IndentLines(ITextEditor editor, int beginLine, int endLine)
		{
			editor.Document.StartUndoableAction();
			TryIndent(editor, beginLine, endLine);
			editor.Document.EndUndoableAction();
		}
		
		public override void SurroundSelectionWithComment(ITextEditor editor)
		{
			SurroundSelectionWithSingleLineComment(editor, "//");
		}
		#endregion
		
		static void TryIndent(ITextEditor editor, int begin, int end)
		{
			IDocument doc = editor.Document;
			var tab = editor.Options.IndentationString;
			bool in_header = IsInHeader(doc, begin);
			for(int next_line = begin; next_line <= end; ++next_line){
				var line = doc.GetLine(next_line);
				bool is_pos_stmt = pos_stmt_searcher.IsMatch(line.Text);
				if(in_header && is_pos_stmt)	//See if we are currently at the first position statement
					in_header = false;			//If so, mark the header section ends.
				
				if(in_header)
					continue;
				
				var new_line_text = is_pos_stmt ? line.Text.Trim() : tab + line.Text.Trim();
				doc.SmartReplaceLine(line, new_line_text);
			}
		}
		
		static bool IsInHeader(IDocument doc, int max)
		{
			bool result = true;
			for(int i = 1; i < max; ++i){
				var line_str = doc.GetLine(i).Text;
				if(pos_stmt_searcher.IsMatch(line_str)){
					result = false;
					break;
				}
			}
			
			return result;
		}
	}
}
