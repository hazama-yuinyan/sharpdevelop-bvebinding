/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/02/21
 * Time: 15:28
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BVE5Language.Ast;
using ICSharpCode.NRefactory;

namespace BVE5Language.Parser
{
	/// <summary>
	/// Description of BVE5CommonParser.
	/// </summary>
	public class BVE5CommonParser
	{
		private static object parse_lock = new object();
		
		private readonly string header_str;
		private readonly string file_kind_name;
		
		/// <summary>
		/// Initializes a new instance of <see cref="BVE5Language.Parser.CommonParser"/>.
		/// </summary>
		/// <param name="headerString">The header text that will be verified if parseHeader option is specified.</param>
		/// <param name="fileKindName">The file type name of which the file is. This will be used for displaying type-specific errors.</param>
		public BVE5CommonParser(string headerString, string fileKindName)
		{
			header_str = headerString;
			file_kind_name = fileKindName;
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
		/// Parses a route file comsuming the given stream as the program code source.
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
                var res = tree.Body[0];
                res.ResetParent();
                return res;
            }else{
                return tree;
            }
		}
		#endregion
		
		#region Implementation details
		private SyntaxTree ParseImpl(string src, string fileName, bool parseHeader)
		{
			lock(parse_lock){
				if(!src.EndsWith("\n"))
					src += "\n";
				
				using(var reader = new StringReader(src)){
					var lexer = new BVE5CommonLexer(reader);
					if(parseHeader){
						int cur_line = lexer.CurrentLine;
						var token = lexer.Current;
						if(token.Literal != header_str)
							throw new BVE5ParserException(1, 1, "Invalid " + file_kind_name + " file!");
					}
					
					lexer.Advance();

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
		
		// sequence '\n'
		private Statement ParseStatement(BVE5CommonLexer lexer)
		{
			var seq = ParseSequence(lexer);
			Token token = lexer.Current;
			if(token.Kind != TokenKind.EOL)
				throw new BVE5ParserException(token.Line, token.Column, "Expected EOL but got " + token.Literal + ".");
			
			lexer.Advance();
			return AstNode.MakeStatement(seq, seq.StartLocation, token.EndLoc);
		}
		
		// argument {',' argument}
		private SequenceExpression ParseSequence(BVE5CommonLexer lexer)
		{
			Token token = lexer.Current;
			var start_loc = token.StartLoc;
			Expression expr = null;
			var children = new List<Expression>();
			while(token.Kind != TokenKind.EOF && token.Kind != TokenKind.EOL){
				expr = ParseArgument(lexer);
				children.Add(expr);
				token = lexer.Current;
				if(token.Literal == ","){
					lexer.Advance();
					token = lexer.Current;
				}
			}
			
			token = lexer.Current;
			return AstNode.MakeSequence(children, start_loc, token.StartLoc);
		}
		
		// literal
		private Expression ParseArgument(BVE5CommonLexer lexer)
		{
			Token token = lexer.Current;
			switch(token.Kind){
			case TokenKind.StringLiteral:
				return ParseString(lexer);
				
			case TokenKind.IntegerLiteral:
			case TokenKind.FloatLiteral:
				var la = lexer.Peek;
				if(la.Literal == ":")
					return ParseTimeLiteral(lexer);
				else
                	return ParseLiteral(lexer);
				
			default:
				throw new BVE5ParserException(token.Line, token.Column, "An argument must be a string, a literal or a time format literal!");
			}
		}
		
		// any-string(including file path)
		private LiteralExpression ParseString(BVE5CommonLexer lexer)
		{
			Token token = lexer.Current;
			Debug.Assert(token.Kind == TokenKind.StringLiteral, "Really meant a string?");
			lexer.Advance();
			return AstNode.MakeLiteral(token.Literal, token.StartLoc, lexer.Current.StartLoc);
		}
		
		// number
		private LiteralExpression ParseLiteral(BVE5CommonLexer lexer)
		{
			Token token = lexer.Current;
			Debug.Assert(token.Kind == TokenKind.IntegerLiteral || token.Kind == TokenKind.FloatLiteral, "Really meant a literal?");
			lexer.Advance();
			if(token.Kind == TokenKind.FloatLiteral)
				return AstNode.MakeLiteral(Convert.ToDouble(token.Literal), token.StartLoc, token.EndLoc);
			else
				return AstNode.MakeLiteral(Convert.ToInt32(token.Literal), token.StartLoc, token.EndLoc);
		}

		// number ':' number ':' number
		private TimeFormatLiteral ParseTimeLiteral(BVE5CommonLexer lexer)
		{
			Token token = lexer.Current;
			Debug.Assert(token.Kind == TokenKind.IntegerLiteral, "Really meant a time literal?");
			int[] nums = new int[3];
			Token start_token = lexer.Current;

			for(int i = 0; i < 3; ++i){
				if(token.Kind == TokenKind.EOF)
					throw new BVE5ParserException(token.Line, token.Column, "Unexpected EOF!");

				nums[i] = Convert.ToInt32(token.Literal);

				lexer.Advance();
				if(i == 2) break;
				
				token = lexer.Current;
				if(token.Kind == TokenKind.EOF)
					throw new BVE5ParserException(token.Line, token.Column, "Unexpected EOF!");
				else if(token.Literal != ":")
					throw new BVE5ParserException(token.Line, token.Column, "Expected ':' but got " + token.Literal);

				lexer.Advance();
				token = lexer.Current;
			}

			return AstNode.MakeTimeFormat(nums[0], nums[1], nums[2], start_token.StartLoc, token.StartLoc);
		}
		#endregion
	}
}
