using System;

using ICSharpCode.NRefactory;

namespace BVE5Language.Parser
{
	public enum TokenKind
	{
		/// <summary>
		/// Represents a syntax token(like ',', ';', '(' etc.)
		/// </summary>
		SyntaxToken,

		/// <summary>
		/// Represents a keyword token. Currently there is no other keyword than "let".
		/// </summary>
		KeywordToken,

		/// <summary>
		/// Represents an identifier.
		/// </summary>
		/// <remarks>
		/// The diffrence between the identifier and the string literal is that identifiers must start with a letter.
		/// </remarks>
		Identifier,

		/// <summary>
		/// Represents an integer.
		/// </summary>
		IntegerLiteral,
		
		/// <summary>
		/// Represents a floating point value.
		/// </summary>
		FloatLiteral,
		
		/// <summary>
		/// Represents an arbitrary string.
		/// </summary>
		StringLiteral,
		
		/// <summary>
		/// Represents the end-of-line token.
		/// </summary>
		EOL,
		
		/// <summary>
		/// Represents the end-of-file token.
		/// </summary>
		EOF
	}
	
	public class Token
	{
		TokenKind kind;
		readonly int line, column;			//the line and column number where the token appears
		readonly string token_literal;		//source string for this token
		internal static Token EOF = new Token(-1, -1, "<EOF>", TokenKind.EOF);
		
		public TextLocation StartLoc{
			get{return new TextLocation(line, column);}
		}
		
		public TextLocation EndLoc{
			get{return new TextLocation(line, column + token_literal.Length);}
		}
		
		public int Line{
			get{return line;}
		}
		
		public int Column{
			get{return column;}
		}
		
		public string Literal{
			get{return token_literal;}
		}
		
		public TokenKind Kind{
			get{return kind;}
		}
		
		public Token(int lineNum, int columnNum, string literal, TokenKind tokenKind)
		{
			line = lineNum;
			column = columnNum;
			token_literal = literal;
			kind = tokenKind;
		}
		
		public override string ToString()
		{
			return string.Format("[Token Kind={0}, Literal={1}]", kind, token_literal);
		}

		
		internal static Token GetEOLToken(int lineNum, int columnNum)
		{
			return new Token(lineNum, columnNum, "<EOL>", TokenKind.EOL);
		}
	}
}

