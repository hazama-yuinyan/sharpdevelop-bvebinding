/*
 * Created by SharpDevelop.
 * User: Ryouta
 * Date: 2013/02/13
 * Time: 15:28
 * 
 * For license, please see license.txt.
 */
using System;
using ICSharpCode.SharpDevelop.Editor;

namespace BVEBinding.Formatting
{
	/// <summary>
	/// Description of BVE5FormattingStrategy.
	/// </summary>
	public class BVE5FormattingStrategy : IFormattingStrategy
	{
		public BVE5FormattingStrategy()
		{
		}
		
		#region IFormattingStrategy members
		public void FormatLine(ITextEditor editor, char charTyped)
		{
			throw new NotImplementedException();
		}
		
		public void IndentLine(ITextEditor editor, IDocumentLine line)
		{
			throw new NotImplementedException();
		}
		
		public void IndentLines(ITextEditor editor, int beginLine, int endLine)
		{
			throw new NotImplementedException();
		}
		
		public void SurroundSelectionWithComment(ITextEditor editor)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}
