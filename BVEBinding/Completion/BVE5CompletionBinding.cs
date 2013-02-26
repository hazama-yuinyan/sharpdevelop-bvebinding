/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/02/21
 * Time: 11:15
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using BVE5Language;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Editor.CodeCompletion;
using ICSharpCode.NRefactory.Completion;

namespace BVEBinding.Completion
{
	/// <summary>
	/// Description of BVE5CompletionBinding.
	/// </summary>
	public class BVE5CompletionBinding : ICodeCompletionBinding
	{
		public CodeCompletionKeyPressResult HandleKeyPress(ITextEditor editor, char ch)
		{
			int cursor_offset = editor.Caret.Offset;
			if(ch == '['){
				var line = editor.Document.GetLineForOffset(cursor_offset);
			}else if(ch == '.'){
				
			}
			
			if(char.IsLetter(ch) && CodeCompletionOptions.CompleteWhenTyping){
				var builtin_type_names = BVE5ResourceManager.GetAllTypeNames();
				
			}
			
			return CodeCompletionKeyPressResult.None;
		}
		
		public bool CtrlSpace(ITextEditor editor)
		{
			throw new NotImplementedException();
		}
	}
}
