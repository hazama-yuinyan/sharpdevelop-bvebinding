/*
 * Created by SharpDevelop.
 * User: Ryouta
 * Date: 2013/02/20
 * Time: 2:12
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

using ICSharpCode.SharpDevelop.Editor;

namespace BVEBinding
{
	/// <summary>
	/// Searches matching brackets for BVE5.
	/// </summary>
	public class BVE5BracketSearcher : IBracketSearcher
	{
		const string openingBrackets = "([";
		const string closingBrackets = ")]";
		
		public BracketSearchResult SearchBracket(IDocument document, int offset)
		{
			if(offset > 0){
				char c = document.GetCharAt(offset - 1);
				int index = openingBrackets.IndexOf(c);
				int other_offset = -1;
				if(index > -1)
					other_offset = SearchBracketForward(document, offset, openingBrackets[index], closingBrackets[index]);
				
				index = closingBrackets.IndexOf(c);
				if(index > -1)
					other_offset = SearchBracketBackward(document, offset - 2, openingBrackets[index], closingBrackets[index]);
				
				if(other_offset > -1)
					return new BracketSearchResult(Math.Min(offset - 1, other_offset), 1,
					                               Math.Max(offset - 1, other_offset), 1);
			}
			
			return null;
		}
		
		#region SearchBracketBackward
		int SearchBracketBackward(IDocument document, int offset, char openBracket, char closingBracket)
		{
			if (offset + 1 >= document.TextLength) return -1;
			// this method parses a BVE5 route file backwards to find the matching bracket
			
			int brackets = -1;
			for(int i = offset; i >= 0; --i){
				char ch = document.GetCharAt(i);
				if(ch == openBracket){
					++brackets;
					if(brackets == 0) return i;
				}else if(ch == closingBracket){
					--brackets;
				}else if(ch == '/' && i > 0){
					if(document.GetCharAt(i - 1) == '/') break;
				}
			}
			return -1;
		}
		#endregion
		
		#region SearchBracketForward
		int SearchBracketForward(IDocument document, int offset, char openBracket, char closingBracket)
		{
			int brackets = 1;
			for(int i = offset; i < document.TextLength; ++i){
				char ch = document.GetCharAt(i);
				if(ch == openBracket){
					++brackets;
				}else if(ch == closingBracket){
					--brackets;
					if (brackets == 0) return i;
				}else if(ch == '/' && i > 0){
					if (document.GetCharAt(i - 1) == '/') break;
				}
			}
			return -1;
		}
		#endregion
	}
}
