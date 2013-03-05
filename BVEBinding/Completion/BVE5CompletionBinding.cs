/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/02/21
 * Time: 11:15
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Linq;
using BVE5Language;
using BVE5Language.TypeSystem;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Editor.CodeCompletion;
using ICSharpCode.NRefactory.Completion;

namespace BVEBinding.Completion
{
	/// <summary>
	/// Code Completion binding for BVE5 files.
	/// </summary>
	public class BVE5CodeCompletionBinding : ICodeCompletionBinding
	{
		public CodeCompletionKeyPressResult HandleKeyPress(ITextEditor editor, char ch)
		{
			int cursor_offset = editor.Caret.Offset;
			if(ch == '['){
				var line = editor.Document.GetLineForOffset(cursor_offset);
			}else if(ch == '.'){
				var line = editor.Document.GetLineForOffset(cursor_offset);
				var type_name = line.Text.Trim();
				var builtin_asm = BVEBuiltins.GetBuiltinAssembly();
				foreach(var type_def in builtin_asm.TopLevelTypeDefinitions){
					if(type_def.Name == type_name){
						var names = type_def.Members.Select(member => member.Name);
						var list = CompletionDataHelper.CreateListFromString(names);
						editor.ShowCompletionWindow(list);
						return CodeCompletionKeyPressResult.Completed;
					}
				}
			}
			
			if(char.IsLetter(ch) && CodeCompletionOptions.CompleteWhenTyping){
				var builtin_type_names = BVE5ResourceManager.GetAllTypeNames();
				var list = CompletionDataHelper.CreateListFromString(builtin_type_names);
				editor.ShowCompletionWindow(list);
				return CodeCompletionKeyPressResult.CompletedIncludeKeyInCompletion;
			}
			
			return CodeCompletionKeyPressResult.None;
		}
		
		public bool CtrlSpace(ITextEditor editor)
		{
			throw new NotImplementedException();
		}
	}
}
