/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/02/21
 * Time: 15:57
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;

namespace BVE5Language.Parser
{
	/// <summary>
	/// Description of InitFileLexer.
	/// </summary>
	public class InitFileLexer : ILexer
	{
		readonly TextReader reader;
		Token token = null,		//current token
			la = null;					//lookahead token
		int line_num = 1, column_num = 1;
		const char EOF = unchecked((char)-1);

		/// <inheritdoc/>
		public Token Current{
			get{return token;}
		}

		/// <inheritdoc/>
		public int CurrentLine{
			get{return token.Line;}
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="BVE5Language.Parser.InitFileLexer"/> class.
		/// </summary>
		/// <remarks>
		/// It assumes that the source string doesn't contain any \r.
		/// Otherwise it fails to tokenize.
		/// </remarks>				
		/// <param name='srcReader'>
		/// Source reader.
		/// </param>
		public InitFileLexer(TextReader srcReader)
		{
			if(srcReader == null)
				throw new ArgumentNullException("srcReader");

			reader = srcReader;
			LookAheadToken();
		}
		
		/// <inheritdoc/>
		public void SetInitialLocation(int line, int column)
		{
			if(token != null)
				throw new InvalidOperationException("SetInitialLocation should not be called after Advance is called");
			
			line_num = line;
			column_num = column;
			la = new Token(line, column, la.Literal, la.Kind);
		}
		
		/// <inheritdoc/>
		public bool HitEOF()
		{
			return token == Token.EOF;
		}

		/// <inheritdoc/>
		public void Advance()
		{
			token = la;
			if(la == Token.EOF)
				return;
			
			LookAheadToken();
		}

		void LookAheadToken()
		{
			SkipWhitespace();
			
			char ch = PeekChar();
			switch(ch){
			case EOF:
				la = Token.EOF;
				break;
				
			case '[':
			case ']':
			case '=':
			case ',':
				GetChar();
				la = new Token(line_num, column_num, ch.ToString(), TokenKind.SyntaxToken);
				++column_num;
				break;
				
			case '\n':
				GetChar();
				la = Token.GetEOLToken(line_num, column_num);
				++line_num;
				column_num = 1;
				break;
				
			case '-':
				GetChar();
				la = GetIdOrNumber(true);
				break;
				
			default:
				la = GetIdOrNumber(false);
				break;
			}
		}
		
		Token GetIdOrNumber(bool canBeNegativeNumber)
		{
			char ch = PeekChar();
			Debug.Assert(ch != EOF && !IsIdTerminator(ch), "Really meant an string or number?");
			bool found_non_digit = false, found_dot = false;
			var sb = new StringBuilder(canBeNegativeNumber ? "-" : "");
			while(ch != EOF && (!IsIdTerminator(ch))){
				if(!found_non_digit && ch != '.' && !char.IsDigit(ch))
					found_non_digit = true;
				
				if(ch == '.'){
					if(found_dot)
						throw new BVE5ParserException(line_num, column_num, "A number literal can't have multiple dots in it!");

					found_dot = true;
				}
				sb.Append(ch);
				GetChar();
				ch = PeekChar();
			}
			
			var tmp = found_non_digit ? new Token(line_num, column_num, sb.ToString(), TokenKind.Identifier) :
				new Token(line_num, column_num, sb.ToString(), found_dot ? TokenKind.FloatLiteral : TokenKind.IntegerLiteral);
			column_num += sb.Length;
			return tmp;
		}
		
		static readonly char[] IdTerminator = new[]{'=', ']', ','};

		static bool IsIdTerminator(char c)
		{
			return IdTerminator.Contains(c) || c < (char)33;
		}
		
		void SkipWhitespace()
		{
			char ch = PeekChar();
			while(char.IsWhiteSpace(ch) || ch == ';'){
				if(ch == ';'){
					do{
						GetChar();
						ch = PeekChar();
					}while(ch != EOF && ch != '\n');
					++line_num;
					column_num = 1;
					GetChar();
					ch = PeekChar();
				}else{
					if(ch == '\n')
						break;
					else
						++column_num;
					
					GetChar();
					ch = PeekChar();
				}
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
	}
}
