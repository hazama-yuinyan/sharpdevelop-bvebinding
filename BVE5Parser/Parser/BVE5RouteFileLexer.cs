using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text;

using ICSharpCode.NRefactory;

namespace BVE5Language.Parser
{
	public class BVE5RouteFileLexer : ILexer
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
		/// Peeks the next token without advancing the token position.
		/// </summary>
		/// <value>
		/// The peeked token.
		/// </value>
		public Token Peek{
			get{return la;}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BVE5Language.Parser.BVE5RouteFileLexer"/> class.
		/// </summary>
		/// <remarks>
		/// It assumes that the source string doesn't contain any \r.
		/// Otherwise it fails to tokenize.
		/// </remarks>				
		/// <param name='srcReader'>
		/// Source reader.
		/// </param>
		public BVE5RouteFileLexer(TextReader srcReader)
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
				throw new InvalidOperationException("SetInitialLocation should not be called after Advance is called for the first time");
			
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
				
			case '(':
			case ')':
            case ',':
			case '.':
			case ';':
			case ':':
            case '[':
            case ']':
			case '=':
				GetChar();
				la = new Token(line_num, column_num, ch.ToString(), TokenKind.SyntaxToken);
				++column_num;
				break;
				
			case '-':
				la = GetIdOrNumber();
				break;
				
			case '0': case '1': case '2': case '3': case '4':
			case '5': case '6': case '7': case '8': case '9':
				la = GetNumber(true);
				break;
				
			default:
				la = GetIdOrKeyword();
				break;
			}
		}

		Token GetIdOrNumber()
		{
			char ch = GetChar();
			Debug.Assert(ch != EOF || ch != '-', "Attempted to parse an ID or number without hyphen.");
			ch = PeekChar();

			if(IsNumChar(ch))
				return GetNumber(false);
			else
				return GetIdOrKeyword();
		}

		Token GetNumber(bool isPositive)
		{
			char c = PeekChar();
			Debug.Assert(c != EOF || !IsNumChar(c), "Really meant a number?");

			var sb = new StringBuilder(isPositive ? "" : "-");
			bool found_dot = false;
			var start_column_num = column_num;

			while(c != EOF && IsNumChar(c) || c == '.'){
				if(c == '.'){
					if(found_dot)
						throw new BVE5ParserException(line_num, column_num, "A number literal can't have multiple dots in it!");

					found_dot = true;
				}
				sb.Append(c);
				GetChar();
				++column_num;
				c = PeekChar();
			}

			return new Token(line_num, start_column_num, sb.ToString(),
			                 found_dot ? TokenKind.FloatLiteral : TokenKind.IntegerLiteral);
		}

		// GetId has first param to handle call from GetIdOrNumber where
		// ID started with a hyphen (and looked like a number). Didn't want to
		// add hyphen to StartId test since it might appear as the keyword
		// minus in the future. Usually the overload without the first param is called.
		//
		// Must not call when the next char is EOF.
		//
		Token GetIdOrKeyword()
		{
			return GetIdOrKeyword(GetChar());
		}

		Token GetIdOrKeyword(char first)
		{
			Debug.Assert(first != EOF && !IsIdTerminator(first), "Really meant an id?");
			var sb = new StringBuilder(first.ToString());
			// See if there's more chars to Id
			char ch = PeekChar();
			while(ch != EOF && (!IsIdTerminator(ch))){
				sb.Append(ch);
				GetChar();
				ch = PeekChar();
			}
			var str = sb.ToString();
			var tmp = new Token(line_num, column_num, str, (str == "let") ? TokenKind.KeywordToken : TokenKind.Identifier);
			column_num += sb.Length;
			return tmp;
		}

		static readonly char[] IdTerminators = new[]{'(', ')', '[', ']', ';', ',', '.'};

		static bool IsIdTerminator(char c)
		{
			return IdTerminators.Contains(c) || (c < (char)33);
		}

		void SkipWhitespace()
		{
			char ch = PeekChar();
			while(char.IsWhiteSpace(ch) || ch == '/'){
				if(ch == '/'){	//skip to the next line since there is no other token in BVE5 Route file that starts with '/'
					do{
						GetChar();
						ch = PeekChar();
					}while(ch != EOF && ch != '\n');
					++line_num;
					column_num = 1;
					GetChar();
					ch = PeekChar();
				}else{
					if(ch == '\n'){
						++line_num;
						column_num = 1;
					}else{
						++column_num;
					}
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

		static bool IsNumChar(char c)
		{
			return '0' <= c && c <= '9';
		}
	}
}

