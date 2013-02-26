/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/02/21
 * Time: 15:30
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace BVE5Language.Parser
{
	/// <summary>
	/// Description of BVE5CommonLexer.
	/// </summary>
	internal class BVE5CommonLexer
	{
		private readonly TextReader reader;
		private Token token = null,		//current token
			la = null;					//lookahead token
		private int line_num = 1, column_num = 1;
		private const char EOF = unchecked((char)-1);

		public Token Current{
			get{return token;}
		}
		
		public Token Peek{
			get{return la;}
		}

		public int CurrentLine{
			get{return token.Line;}
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="BVE5Language.Parser.BVE5CommonLexer"/> class.
		/// </summary>
		/// <remarks>
		/// It assumes that the source string doesn't contain any \r.
		/// Otherwise it fails to tokenize.
		/// </remarks>				
		/// <param name='srcReader'>
		/// Source reader.
		/// </param>
		public BVE5CommonLexer(TextReader srcReader)
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

		private void LookAheadToken()
		{
			SkipWhitespace();
			
			char ch = PeekChar();
			switch(ch){
			case EOF:
				la = null;
				break;
				
			case ',':
			case ':':
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
				la = GetStringOrNumber(true);
				break;
				
			default:
				la = GetStringOrNumber(false);
				break;
			}
		}
		
		private Token GetStringOrNumber(bool canBeNegativeNumber)
		{
			char ch = PeekChar();
			Debug.Assert(ch != EOF && !IsStringTerminator(ch), "Really meant an string or number?");
			bool found_non_digit = false, found_dot = false;
			var sb = new StringBuilder(canBeNegativeNumber ? "-" : "");
			while(ch != EOF && (!IsStringTerminator(ch))){
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
			
			var tmp = found_non_digit ? new Token(line_num, column_num, sb.ToString(), TokenKind.StringLiteral) :
				new Token(line_num, column_num, sb.ToString(), found_dot ? TokenKind.FloatLiteral : TokenKind.IntegerLiteral);
			column_num += sb.Length;
			return tmp;
		}
		
		private static readonly char[] string_terminators = new[]{',', ':'};

		private static bool IsStringTerminator(char c)
		{
			return string_terminators.Contains(c) || c < (char)20;
		}
		
		private void SkipWhitespace()
		{
			char ch = PeekChar();
			while(char.IsWhiteSpace(ch) || ch == '#'){
				if(ch == '#'){
					do{
						GetChar();
						ch = PeekChar();
					}while(ch != EOF && ch != '\n');
					GetChar();
					++line_num;
					column_num = 1;
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
		
		private char GetChar()
		{
			return unchecked((char)reader.Read());
		}

		private char PeekChar()
		{
			return unchecked((char)reader.Peek());
		}
	}
}
