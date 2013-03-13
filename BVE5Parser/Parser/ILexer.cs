/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/03/09
 * Time: 16:42
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace BVE5Language.Parser
{
	/// <summary>
	/// Interface for all lexers.
	/// </summary>
	public interface ILexer
	{
		/// <summary>
		/// Gets the token at the current lexer position.
		/// </summary>
		Token Current{get;}
		
		/// <summary>
		/// Gets the line number of the current lexer position.
		/// </summary>
		int CurrentLine{get;}
		
		/// <summary>
		/// Sets the initial location of the lexer. It must be called before Advance is called for the first time.
		/// </summary>
		/// <param name="line"></param>
		/// <param name="column"></param>
		void SetInitialLocation(int line, int column);
		
		/// <summary>
		/// Checks whether the lexer hits the EOF.
		/// </summary>
		/// <returns>
		/// <c>true</c>, if EOF was encountered, <c>false</c> otherwise.
		/// </returns>
		bool HitEOF();
		
		/// <summary>
		/// Tells the lexer to read the next token and to store it on the internal buffer.
		/// </summary>
		void Advance();
	}
}
