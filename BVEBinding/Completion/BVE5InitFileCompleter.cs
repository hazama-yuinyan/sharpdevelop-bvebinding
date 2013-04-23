/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/03/09
 * Time: 15:11
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using BVE5Language;
using BVE5Language.Ast;
using BVE5Language.Parser;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Editor.CodeCompletion;

namespace BVE5Binding.Completion
{
	/// <summary>
	/// Code completer for BVE5 init file.
	/// </summary>
	public class BVE5InitFileCompleter : ICodeCompleter
	{
		readonly Dictionary<string, InitFileMemberAnnotation> SemanticInfo;
		InitFileParser parser;
		
		internal BVE5InitFileCompleter(BVE5FileKind kind)
		{
			SemanticInfo = BVE5ResourceManager.InitFileSemanticInfos[kind.ToString()].SemanticInfos;
			parser = ParserFactory.CreateInitFileParser(kind);
		}
		
		public Tuple<bool, CodeCompletionKeyPressResult> TryComplete(ITextEditor editor, char ch, IInsightWindowHandler insightWindowHandler)
		{
			int cursor_offset = editor.Caret.Offset;
			if(ch == '['){
				var list = CompletionDataHelper.GenerateCompletionList(SemanticInfo.Keys.ToList(),
				                                                       SemanticInfo.Select(info => BVE5ResourceManager.GetDocumentationString(info.Value.Doc)).ToList(),
				                                                      null);
				editor.ShowCompletionWindow(list);
				return Tuple.Create(true, CodeCompletionKeyPressResult.Completed);
			}else if(char.IsLetter(ch)){
				var caret_line_num = editor.Caret.Line;
				var tree = parser.Parse(editor.Document.GetText(0, editor.Document.PositionToOffset(caret_line_num, 1)), "<string>", true);
				var section_stmts = tree.FindNodes(node => node.Type == NodeType.SectionStmt).OfType<SectionStatement>();	//retrieve all section statements up to the current caret position
				var context_stmt = section_stmts.LastOrDefault();
				
				//if the context statement is null, it must be in a vehicle parameters file
				var section_name = (context_stmt != null) ? context_stmt.SectionName.Name : "Global";
				var section_semantic_info = SemanticInfo[section_name];
				var list = CompletionDataHelper.GenerateCompletionList(section_semantic_info.Keys.Select(key => key.Name).ToList(),
				                                                       section_semantic_info.Keys.Select(key => BVE5ResourceManager.GetDocumentationString(key.Doc)).ToList(),
				                                                      null);
				editor.ShowCompletionWindow(list);
				return Tuple.Create(true, CodeCompletionKeyPressResult.CompletedIncludeKeyInCompletion);
			}
			
			return Tuple.Create(false, CodeCompletionKeyPressResult.None);
		}
	}
}
