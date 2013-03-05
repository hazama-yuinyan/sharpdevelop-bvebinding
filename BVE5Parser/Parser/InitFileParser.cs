/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/02/21
 * Time: 15:57
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BVE5Language.Ast;
using ICSharpCode.NRefactory;

namespace BVE5Language.Parser
{
	/// <summary>
	/// Description of InitFileParser.
	/// </summary>
	public class InitFileParser
	{
		static object parse_lock = new object();
		
		readonly string[] HeaderStringSplit;
		readonly string FileTypeName;
		
		/// <summary>
		/// Initializes a new instance of <see cref="BVE5Language.Parser.InitFileParser"/>.
		/// </summary>
		/// <param name="headerString">The header text that will be verified if parseHeader option is specified.</param>
		/// <param name="fileTypeName">The file type name of which the file is. This will be used for displaying type-specific errors.</param>
		public InitFileParser(string headerString, string fileTypeName)
		{
			HeaderStringSplit = headerString.Split(' ');
			FileTypeName = fileTypeName;
		}
		
		#region public surface
		/// <summary>
		/// Parses the specified file at filePath.
		/// </summary>
		/// <param name='filePath'>
		/// Path to the file being parsed.
		/// </param>
		public SyntaxTree Parse(string filePath)
		{
			return ParseImpl(File.ReadAllText(filePath).Replace(Environment.NewLine, "\n"), filePath, true);
		}

		/// <summary>
		/// Parses a string.
		/// </summary>
		/// <param name='programSrc'>
		/// Program source in string format.
		/// </param>
		/// <param name='fileName'>
		/// File name. This is used for the SyntaxTree node.
		/// </param>
		public SyntaxTree Parse(string programSrc, string fileName = "")
		{
			return ParseImpl(programSrc.Replace(Environment.NewLine, "\n"), fileName, false);
		}

		/// <summary>
		/// Parses an init file comsuming the given stream as the program code source.
		/// </summary>
		/// <param name='stream'>
		/// Stream.
		/// </param>
		/// <param name='fileName'>
		/// File name. This is used for the SyntaxTree node.
		/// </param>
		public SyntaxTree Parse(Stream stream, string fileName = "")
		{
			using(var reader = new StreamReader(stream)){
				return ParseImpl(reader.ReadToEnd().Replace(Environment.NewLine, "\n"), fileName, true);
			}
		}

		/// <summary>
		/// Parses a statement. This is especially intended for real-time parsing such as those which occur during
		/// real-time analysis for text editor support.
		/// </summary>
		/// <param name='src'>
		/// Source string.
		/// </param>
        /// <param name="returnAsSyntaxTree">
        /// Flag determining whether the method should return the result as SyntaxTree instance or not.
        /// </param>
		public AstNode ParseOneStatement(string src, bool returnAsSyntaxTree = false)
		{
			var tree = ParseImpl(src.Replace(Environment.NewLine, "\n"), "<string>", false);
            if(!returnAsSyntaxTree){
				var res = tree.Body.First();
                res.Remove();
                return res;
            }else{
                return tree;
            }
		}
		#endregion
		
		#region Implementation details
		SyntaxTree ParseImpl(string src, string fileName, bool parseHeader)
		{
			lock(parse_lock){
				if(!src.EndsWith("\n"))
					src += "\n";
				
				using(var reader = new StringReader(src)){
					var lexer = new InitFileLexer(reader);
					if(parseHeader){
						int cur_line = lexer.CurrentLine;
						var tokens = new List<Token>();
						while(lexer.Current != Token.EOF && lexer.CurrentLine == cur_line){
							lexer.Advance();
							tokens.Add(lexer.Current);
						}
						if(tokens.Count != 3 || tokens[0].Literal != HeaderStringSplit[0] || tokens[1].Literal != HeaderStringSplit[1] ||
						   tokens[2].Literal != HeaderStringSplit[2])
							throw new BVE5ParserException(1, 1, "Invalid " + FileTypeName + " file!");
					}else{
						lexer.Advance();
					}

					BVE5Language.Ast.Statement stmt = null;
					var stmts = new List<BVE5Language.Ast.Statement>();
					while(lexer.Current != Token.EOF){
						stmt = ParseStatement(lexer);
						stmts.Add(stmt);
					}

					return AstNode.MakeSyntaxTree(stmts, fileName, new TextLocation(1, 1), stmt.EndLocation);
				}
			}
		}
		
		// section-statement | definition '\n'
		BVE5Language.Ast.Statement ParseStatement(InitFileLexer lexer)
		{
			Token token = lexer.Current;
			BVE5Language.Ast.Expression expr = null;
			
			if(token.Kind == TokenKind.Identifier){
				expr = ParseDefinition(lexer);
			}else if(token.Literal == "["){
				return ParseSectionStatement(lexer);
			}else{
				throw new BVE5ParserException(token.Line, token.Column,
					                          "A statement must start with an identifier or the sign '['.");
			}
			
			token = lexer.Current;
			if(token.Kind != TokenKind.EOL)
				throw new BVE5ParserException(token.Line, token.Column, "Expected EOL but got " + token.Literal + ".");
			
			lexer.Advance();
			return AstNode.MakeStatement(expr, expr.StartLocation, lexer.Current.StartLoc);
		}
		
		// '[' identifier ']' '\n'
		SectionStatement ParseSectionStatement(InitFileLexer lexer)
		{
			Token token = lexer.Current;
			var start_loc = token.StartLoc;
			Debug.Assert(token.Literal == "[", "Really meant a section statement?");
			
			lexer.Advance();
			Identifier ident = ParseIdent(lexer);
			token = lexer.Current;
			if(token.Literal != "]")
				throw new BVE5ParserException(token.Line, token.Column, "Expected ']' but got " + token.Literal + ".");
			
			lexer.Advance();
			token = lexer.Current;
			if(token.Kind != TokenKind.EOL)
				throw new BVE5ParserException(token.Line, token.Column, "Expected EOL but got " + token.Literal + ".");
			
			lexer.Advance();
			token = lexer.Current;
			return AstNode.MakeSectionStatement(ident, start_loc, token.StartLoc);
		}
		
		// identifier '=' sequence
		DefinitionExpression ParseDefinition(InitFileLexer lexer)
		{
			Identifier lhs = ParseIdent(lexer);
			Token token = lexer.Current;
			if(token.Literal != "=")
				throw new BVE5ParserException(token.Line, token.Column, "Expected '=' but got " + token.Literal + ".");
			
			lexer.Advance();
			SequenceExpression rhs = ParseSequence(lexer);
			Expression expr = rhs;
			if(rhs.Expressions.Count() == 1){
				var tmp = rhs.Expressions.First();
				tmp.Remove();
				expr = tmp;
			}
			return AstNode.MakeDefinition(lhs, expr, lhs.StartLocation, lexer.Current.StartLoc);
		}
		
		// expr {',' expr}
		SequenceExpression ParseSequence(InitFileLexer lexer)
		{
			Token token = lexer.Current;
			var start_loc = token.StartLoc;
			Expression expr = null;
			var exprs = new List<Expression>();
			while(token.Kind != TokenKind.EOL){
				switch(token.Kind){
				case TokenKind.Identifier:
					if(token.Literal.Contains("."))
						expr = ParsePathLiteral(lexer);
					else
						expr = ParseColorLiteral(lexer);
					
					break;
					
				case TokenKind.IntegerLiteral:
				case TokenKind.FloatLiteral:
					expr = ParseLiteral(lexer);
					break;
					
				default:
					throw new BVE5ParserException("Line {0}: Invalid definition!", token.Line);
				}
				
				exprs.Add(expr);
				token = lexer.Current;
				if(token.Literal == ","){
					lexer.Advance();
					token = lexer.Current;
				}
			}
			
			return AstNode.MakeSequence(exprs, start_loc, token.StartLoc);
		}
		
		// anyCharacterExcept-'='-']'-','
		Identifier ParseIdent(InitFileLexer lexer)
		{
			Debug.Assert(lexer.Current.Kind == TokenKind.Identifier, "Really meant an identifier?");
			Token token = lexer.Current;
			lexer.Advance();
			return AstNode.MakeIdent(token.Literal, token.StartLoc, lexer.Current.StartLoc);
		}
		
		// path-literal
        LiteralExpression ParsePathLiteral(InitFileLexer lexer)
        {
            Token token = lexer.Current;
            var start_loc = token.StartLoc;
            Debug.Assert(token.Kind == TokenKind.Identifier, "Really meant a path literal?");
            var sb = new StringBuilder();

            while(token.Kind != TokenKind.EOL && token.Literal != ","){
                sb.Append(token.Literal);
                lexer.Advance();
                token = lexer.Current;
            }
            return AstNode.MakeLiteral(sb.ToString(), start_loc, token.StartLoc);
        }
        
        // '#' {number}{3,6}
        LiteralExpression ParseColorLiteral(InitFileLexer lexer)
        {
        	Token token = lexer.Current;
        	Debug.Assert(token.Literal.StartsWith("#"), "Really meant a color literal?");
        	lexer.Advance();
        	return AstNode.MakeLiteral(token.Literal, token.StartLoc, lexer.Current.StartLoc);
        }

		// number
		LiteralExpression ParseLiteral(InitFileLexer lexer)
		{
			Token token = lexer.Current;
			Debug.Assert(token.Kind == TokenKind.IntegerLiteral || token.Kind == TokenKind.FloatLiteral, "Really meant a literal?");
			lexer.Advance();
			if(token.Kind == TokenKind.FloatLiteral)
				return AstNode.MakeLiteral(Convert.ToDouble(token.Literal), token.StartLoc, token.EndLoc);
			else
				return AstNode.MakeLiteral(Convert.ToInt32(token.Literal), token.StartLoc, token.EndLoc);
		}
		#endregion
	}
}
