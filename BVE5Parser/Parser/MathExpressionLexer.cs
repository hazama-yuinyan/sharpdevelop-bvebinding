/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/03/02
 * Time: 14:28
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using BVE5Language.Ast;

namespace BVE5Language.Parser.Extension
{
	/// <summary>
	/// The lexer for MathExpressionParser.
	/// </summary>
	/*internal class MathExpressionLexer
	{
		readonly TextReader reader;
		Token token = null,		//current token
			la = null;					//lookahead token
		int line_num = 1, column_num = 1;
		const char EOF = unchecked((char)-1);

		public Token Current{
			get{return token;}
		}

		public int CurrentLine{
			get{return token.Line;}
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="BVE5Language.Parser.Extension.MathExpressionLexer"/> class.
		/// </summary>
		/// <remarks>
		/// It assumes that the source string doesn't contain any \r.
		/// Otherwise it fails to tokenize.
		/// </remarks>				
		/// <param name='srcReader'>
		/// Source reader.
		/// </param>
		public MathExpressionLexer(TextReader srcReader)
		{
			if(srcReader == null)
				throw new ArgumentNullException("srcReader");

			reader = srcReader;
			LookAheadToken();
		}
		
		/// <summary>
		/// Checks whether the lexer hits the EOF.
		/// </summary>
		/// <returns>
		/// <c>true</c>, if EOF was encountered, <c>false</c> otherwise.
		/// </returns>
		public bool HitEOF()
		{
			return token == Token.EOF;
		}

		/// <summary>
		/// Tells the lexer to read the next token and to store it on the internal buffer.
		/// </summary>
		public void Advance()
		{
			if(la != null){
				token = la;
			}else{
				token = Token.EOF;
				return;
			}

			LookAheadToken();
		}

		void LookAheadToken()
		{
			SkipWhitespace();
			
			char ch = PeekChar();
			switch(ch){
			case EOF:
				la = null;
				break;
				
			case '(':
			case ')':
			case '=':
			case '+':
			case '-':
			case '*':
			case '/':
			case '$':
			case '{':
			case '}':
				GetChar();
				la = new Token(line_num, column_num, ch.ToString(), TokenKind.SyntaxToken);
				++column_num;
				break;
				
			case '-':
				GetChar();
				la = GetNumber(true);
				break;
				
			case '0': case '1': case '2': case '3': case '4':
			case '5': case '6': case '7': case '8': case '9':
				la = GetNumber(false);
				break;
				
			default:
				throw new BVE5ParserException("Unknown token");
				//la = GetId();
				//break;
			}
		}
		
		Token GetNumber(bool isNegativeNumber)
		{
			char ch = PeekChar();
			Debug.Assert(ch != EOF && char.IsDigit(ch), "Really meant a number?");
			var sb = new StringBuilder(isNegativeNumber ? "-" : "");
			bool found_dot = false;
			while(ch != EOF && char.IsDigit(ch) || ch == '.'){
				if(ch == '.'){
					if(found_dot)
						throw new BVE5ParserException(line_num, column_num, "A number literal can't have multiple dots in it!");
					
					found_dot = true;
				}
				sb.Append(ch);
				GetChar();
				ch = PeekChar();
			}
			
			var res = AstNode.MakeLiteral(Convert.ToDouble(sb.ToString()), line_num, column_num);
			column_num += sb.Length;
			return res;
		}
		
		Token GetId()
		{
			char ch = PeekChar();
			Debug.Assert(ch != EOF && !IsIdTerminator(ch), "Really meant an string or number?");
			var sb = new StringBuilder();
			while(ch != EOF && (!IsIdTerminator(ch))){
				sb.Append(ch);
				GetChar();
				ch = PeekChar();
			}
			
			var tmp = new Token(line_num, column_num, sb.ToString(), TokenKind.Identifier);
			column_num += sb.Length;
			return tmp;
		}
		
		static bool IsIdTerminator(char c)
		{
			return c == '}' || c < (char)33;
		}
		
		void SkipWhitespace()
		{
			char ch = PeekChar();
			while(char.IsWhiteSpace(ch)){
				if(ch == '\n')
					break;
				else
					++column_num;
				
				GetChar();
				ch = PeekChar();
			}
		}
		
		char GetChar()
		{
			return unchecked((char)reader.Read());
		}

		char PeekChar()
		{
			return unchecked((char)reader.Peek());
		}
	}*/
}
