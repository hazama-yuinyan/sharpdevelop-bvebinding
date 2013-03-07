﻿//
// BVE5RouteFileParser.cs
//
// Author:
//       HAZAMA <kotonechan@live.jp>
//
// Copyright (c) 2013 HAZAMA
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using BVE5Language.Ast;

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.TypeSystem;


/**
 * BVE5のルートファイルには正式な仕様書のようなものはないが、コードコンプリーションなどの各種機能を実装するためにここに便宜的に仕様を決めることにする。
 * まず、BVE5のルートファイルの書式をEBNFで記すと概ね以下のようになるはずである。
 * 		route-file = meta-header {header} {body} ;
 * 		meta-header = "BveTs Map 1.00" ;
 * 		header = type "." method-name "(" {arguments} ")" ";" ;
 * 		body = non-zero-digit {digit} ";" ["\n"] {content} ;
 * 		content = type ["[" name "]"] "." method-name "(" {arguments} ")" ";" ;
 * 		arguments = name | number | time-literal | file-path ;
 * 		type = name ;
 * 		method-name = name ;
 * 		number = digit {digit} ["." digit {digit}] ;
 * 		name = {any character} ;
 * 		time-literal = number ":" number ":" number ;
 * 		file-path = {any character delimited by "\"} ;
 * 		non-zero-digit = "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" ;
 * 		digit = "0" | non-zero-digit ;
 * BVE5のルートファイルには以下のような型のオブジェクトが出現する。
 * 		Primitive types : int, float, name
 * 		Complex types : TimeFormat, FilePath, enum<Tilt>, enum<Direction>, enum<ForwardingDirection>
 */

/**
 * BVE5 has no official specification of its original file formats. But we will absolutely need them when implementing code completion,
 * refactoring, syntax highlighting etc. so I will define it here.
 * When written in somewhat EBNF-like grammer, the format of route files looks something like the following;
 * 		route-file = meta-header {header} {body} ;
 * 		meta-header = "BveTs Map 1.00" ;
 * 		header = type "." method-name "(" {arguments} ")" ";" ;
 * 		body = non-zero-digit {digit} ";" ["\n"] {content} ;
 * 		content = type ["[" name "]"] "." method-name "(" {arguments} ")" ";" ;
 * 		arguments = name | number | time-literal | file-path ;
 * 		type = name ;
 * 		method-name = name ;
 * 		number = digit {digit} ["." digit {digit}] ;
 * 		name = {any character} ;
 * 		time-literal = number ":" number ":" number ;
 * 		file-path = {any character delimited by "\"} ;
 * 		non-zero-digit = "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" ;
 * 		digit = "0" | non-zero-digit ;
 * 	And I will define the following (buitlin and only) data types;
 * 		Primitive types : int, float, name
 * 		Complex types : TimeFormat, FilePath, enum<Tilt>, enum<Direction>, enum<ForwardingDirection>
 * 	Obviously, unlike common programming languages users cann't create his/her own types. But still they can add some fields to
 * 	the limited number of builtin types, and they can use them for specifying the kind of objects on which the statement must affect.
 */
 
/** TODO: プリプロセッサライクな変数(というか値のエイリアス定義機能)の実装
 * 例： let a = 1;
 * 	Track[Rail1].Put(a, a, a);
 * 	=>	保存時に以下のように置換される
 * 	Track[Rail1].Put(1, 1, 1);
 */


namespace BVE5Language.Parser
{
	/// <summary>
	/// BVE5 route file parser.
	/// </summary>
	public class BVE5RouteFileParser
	{
		internal static object parse_lock = new object();

		public class ErrorReportPrinter : ReportPrinter
		{
			readonly string file_name;
			public readonly List<Error> Errors = new List<Error>();
			
			public ErrorReportPrinter(string fileName)
			{
				this.file_name = fileName;
			}
			
			public override void Print(AbstractMessage msg, bool showFullPath = false)
			{
				base.Print(msg, showFullPath);
				var newError = new Error(msg.IsWarning ? ErrorType.Warning : ErrorType.Error, msg.Text,
                                         new DomRegion(file_name, msg.Location.Line, msg.Location.Column));
				Errors.Add(newError);
			}
		}
		ErrorReportPrinter error_report_printer = new ErrorReportPrinter(null);

		public bool HasErrors {
			get {
				return error_report_printer.ErrorsCount > 0;
			}
		}
		
		public bool HasWarnings {
			get {
				return error_report_printer.WarningsCount > 0;
			}
		}
		
		public IEnumerable<Error> Errors {
			get {
				return error_report_printer.Errors.Where(e => e.ErrorType == ErrorType.Error);
			}
		}
		
		public IEnumerable<Error> Warnings {
			get {
				return error_report_printer.Errors.Where(e => e.ErrorType == ErrorType.Warning);
			}
		}
		
		public IEnumerable<Error> ErrorsAndWarnings {
			get { return error_report_printer.Errors; }
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
		/// <param name='parseHeader'>
		/// Whether it should parse the header.
		/// </param>
		public SyntaxTree Parse(string programSrc, string fileName = "", bool parseHeader = false)
		{
			return ParseImpl(programSrc.Replace(Environment.NewLine, "\n"), fileName, parseHeader);
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
				var res = tree.Body.First();
                res.Remove();
                return res;
            }else{
                return tree;
            }
		}
		#endregion

		#region Implemetation details
		SyntaxTree ParseImpl(string src, string fileName, bool parseHeader)
		{
			lock(parse_lock){
				using(var reader = new StringReader(src)){
					var lexer = new BVE5RouteFileLexer(reader);
					lexer.Advance();
					
					if(parseHeader){
						int cur_line = lexer.CurrentLine;
						var tokens = new List<Token>();
						while(lexer.Current != Token.EOF && lexer.CurrentLine == cur_line){
							tokens.Add(lexer.Current);
							lexer.Advance();
						}
						if(tokens.Count != 3 || tokens[0].Literal != "BveTs" || tokens[1].Literal != "Map" ||
						   tokens[2].Literal != "1.00")
							throw new BVE5ParserException(1, 1, "Invalid Map file!");
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

		// definition ';' | [\d]+ ';' | ident ['[' ident ']'] '.' ident '(' [args] ')' ';'
		BVE5Language.Ast.Statement ParseStatement(BVE5RouteFileLexer lexer)
		{
			Token token = lexer.Current;
			BVE5Language.Ast.Expression expr = null;

			switch(token.Kind){
			case TokenKind.IntegerLiteral:
				expr = ParseLiteral(lexer);
				break;
				
			case TokenKind.Identifier:
				BVE5Language.Ast.Expression res = ParseIdent(lexer);
				if(lexer.Current.Literal == "[")
					res = ParseIndexExpr(lexer, res);

				token = lexer.Current;
				if(token.Literal != ".")
					throw new BVE5ParserException(token.Line, token.Column, "Expected '.' but got " + token.Literal);

				res = ParseMemberRef(lexer, res);
				expr = ParseInvokeExpr(lexer, res);
				break;
				
			case TokenKind.KeywordToken:
				expr = ParseDefinition(lexer);
				break;
				
			default:
				throw new BVE5ParserException(token.Line, token.Column,
					                          "A statement must start with a keyword, an integer literal or an identifier.");
			}

			token = lexer.Current;
			if(token.Literal != ";")
				throw new BVE5ParserException(token.Line, token.Column, "Unexpected character: " + token.Literal);

			lexer.Advance();

			return AstNode.MakeStatement(expr, expr.StartLocation, token.EndLoc);   //token should be pointing to a semicolon token
		}
		
		// "let" ident '=' expr
		DefinitionExpression ParseDefinition(BVE5RouteFileLexer lexer)
		{
			Token token = lexer.Current;
			var start_loc = token.StartLoc;
			Debug.Assert(token.Kind == TokenKind.KeywordToken, "Really meant a definition?");
			if(token.Literal != "let")
				throw new BVE5ParserException(token.Line, token.Column, "Unknown keyword " + token.Literal);
			
			lexer.Advance();
			var lhs = ParseIdent(lexer);
			token = lexer.Current;
			if(token.Literal != "=")
				throw new BVE5ParserException(token.Line, token.Column, "Expected '=' but got " + token.Literal);
			
			lexer.Advance();
			var rhs = ParseRValueExpression(lexer);
			token = lexer.Current;
			return AstNode.MakeDefinition(lhs, rhs, start_loc, token.StartLoc);
		}

		// any-character-except-(-)-.-;-[-]
		Identifier ParseIdent(BVE5RouteFileLexer lexer)
		{
			Token token = lexer.Current;
			Debug.Assert(token.Kind == TokenKind.Identifier, "Really meant an identifier?");
			lexer.Advance();
			return AstNode.MakeIdent(token.Literal, token.StartLoc, lexer.Current.StartLoc);
		}
		
		// ident '[' ident ']'
		IndexerExpression ParseIndexExpr(BVE5RouteFileLexer lexer, BVE5Language.Ast.Expression target)
		{
			Debug.Assert(lexer.Current.Literal == "[", "Really meant an index reference?");
			lexer.Advance();
			Token token = lexer.Current;
			if(token.Kind != TokenKind.Identifier && token.Kind != TokenKind.IntegerLiteral){
				throw new BVE5ParserException(token.Line, token.Column, "Unexpected token: " + token.Literal +
                    "; The operator '[]' can only take a string or an index as its argument.");
            }

			LiteralExpression literal = ParseLiteral(lexer);
			token = lexer.Current;
			if(token.Literal != "]")
				throw new BVE5ParserException(token.Line, token.Column, "Expected ']' but got " + token.Literal);

			lexer.Advance();
			return AstNode.MakeIndexExpr(target, literal, target.StartLocation, token.EndLoc);
		}

		// ident '.' ident
		MemberReferenceExpression ParseMemberRef(BVE5RouteFileLexer lexer, BVE5Language.Ast.Expression parent)
		{
			Debug.Assert(lexer.Current.Literal == ".", "Really meant a member reference?");
			lexer.Advance();
			Token token = lexer.Current;
			if(token.Kind != TokenKind.Identifier)
				throw new BVE5ParserException(token.Line, token.Column, "Unexpected token: " + token.Kind);

			Identifier ident = ParseIdent(lexer);
			return AstNode.MakeMemRef(parent, ident, parent.StartLocation, ident.EndLocation);
		}

		// expr '(' [arguments] ')'
		InvocationExpression ParseInvokeExpr(BVE5RouteFileLexer lexer, BVE5Language.Ast.Expression callTarget)
		{
			Debug.Assert(lexer.Current.Literal == "(", "Really meant an invoke expression?");
			lexer.Advance();
			Token token = lexer.Current;
			var args = new List<BVE5Language.Ast.Expression>();

			while(token.Kind != TokenKind.EOF && token.Literal != ")"){
                args.Add(ParseRValueExpression(lexer));

				token = lexer.Current;
				if(token.Literal == ","){
					lexer.Advance();
					token = lexer.Current;
				}
			}

			if(token.Kind == TokenKind.EOF)
				throw new BVE5ParserException(token.Line, token.Column, "Unexpected EOF!");

            lexer.Advance();
			return AstNode.MakeInvoke(callTarget, args, callTarget.StartLocation, token.EndLoc);    //token should be pointing to a closing parenthesis token
		}

        BVE5Language.Ast.Expression ParseRValueExpression(BVE5RouteFileLexer lexer)
        {
            Token token = lexer.Current;
            switch(token.Kind){
            case TokenKind.Identifier:
                if(lexer.Peek.Literal != "," || lexer.Peek.Literal != ")")
                    return ParsePathLiteral(lexer);
                else
                    return ParseIdent(lexer);

            case TokenKind.IntegerLiteral:
            case TokenKind.FloatLiteral:
                var la = lexer.Peek;
                if(la.Literal == ":")
                    return ParseTimeLiteral(lexer);
                else
                    return ParseLiteral(lexer);
            
            default:
                throw new BVE5ParserException(token.Line, token.Column,
                                              "A right-hand-side expression must be an identifier, a file path, a literal or a time format literal!");
            }
        }

        // path-literal
        LiteralExpression ParsePathLiteral(BVE5RouteFileLexer lexer)
        {
            Token token = lexer.Current;
            var start_loc = token.StartLoc;
            Debug.Assert(token.Kind == TokenKind.Identifier, "Really meant a path literal?");
            var sb = new StringBuilder();

            while(token.Literal != "," && token.Literal != ")"){
                sb.Append(token.Literal);
                lexer.Advance();
                token = lexer.Current;
            }
            return AstNode.MakeLiteral(sb.ToString(), start_loc, token.StartLoc);
        }

		// number | any-string
		LiteralExpression ParseLiteral(BVE5RouteFileLexer lexer)
		{
			Token token = lexer.Current;
			Debug.Assert(token.Kind == TokenKind.IntegerLiteral || token.Kind == TokenKind.FloatLiteral ||
			             token.Kind == TokenKind.Identifier, "Really meant a literal?");
			lexer.Advance();
			
			if(token.Kind == TokenKind.FloatLiteral)
				return AstNode.MakeLiteral(Convert.ToDouble(token.Literal), token.StartLoc, token.EndLoc);
			else if(token.Kind == TokenKind.IntegerLiteral)
				return AstNode.MakeLiteral(Convert.ToInt32(token.Literal), token.StartLoc, token.EndLoc);
			else
				return AstNode.MakeLiteral(token.Literal, token.StartLoc, token.EndLoc);
		}

		// number ':' number ':' number
		TimeFormatLiteral ParseTimeLiteral(BVE5RouteFileLexer lexer)
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

