/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/03/08
 * Time: 14:45
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Editor.CodeCompletion;

namespace BVE5Binding.Completion
{
	/// <summary>
	/// Description of ICodeCompleter.
	/// </summary>
	internal interface ICodeCompleter
	{
		Tuple<bool, CodeCompletionKeyPressResult> TryComplete(ITextEditor editor, char ch, IInsightWindowHandler insightWindowHandler);
	}
}
