//
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
using System.Text.RegularExpressions;
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
 * 		content = type ["[" name "]"] "." method-name "(" {argument} ")" ";" ;
 * 		argument = name | number | time-literal | file-path ;
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
 * 		content = type ["[" name "]"] "." method-name "(" {argument} ")" ";" ;
 * 		argument = name | number | time-literal | file-path ;
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
 
/** TODO: プリプロセッサライクな変数(というか値のエイリアス定義機能)の実装(パース自体は実装済み。あとは保存時の展開処理のみ)
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

		readonly Regex MetaHeaderRegexp = new Regex(@"BveTs Map\s*([\d.]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		ErrorReportPrinter error_report_printer = new ErrorReportPrinter(null);
		bool has_error_reported = false, enable_strict_parsing = true;

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
		
		void AddWarning(ErrorCode warningCode, int line, int column, string message, List<string> extraInfos = null)
		{
			error_report_printer.Print(new WarningMessage((int)warningCode, new TextLocation(line, column), message, extraInfos));
		}
		
		void AddError(ErrorCode errorCode, int line, int column, string message, List<string> extraInfos = null)
		{
			error_report_printer.Print(new ErrorMessage((int)errorCode, new TextLocation(line, column), message, extraInfos));
			has_error_reported = true;
		}
		
		void TryRecovery(BVE5RouteFileLexer lexer)
		{
			var start_line = lexer.CurrentLine;
			Token token = lexer.Current;
			while(token.Kind != TokenKind.EOF){
				if(lexer.CurrentLine != start_line)
					return;
				
				lexer.Advance();
				token = lexer.Current;
			}
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
			enable_strict_parsing = true;
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
			enable_strict_parsing = true;
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
			enable_strict_parsing = true;
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
        /// Whether the method should return the result as a SyntaxTree or not.
        /// </param>
		public AstNode ParseOneStatement(string src, bool returnAsSyntaxTree = false)
		{
			enable_strict_parsing = false;
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
					
					string version_str = "unknown";
					if(parseHeader){
						int cur_line = lexer.CurrentLine;
						var meta_header = new StringBuilder();
						while(lexer.Current != Token.EOF && lexer.CurrentLine == cur_line){
							meta_header.Append(lexer.Current.Literal);
							meta_header.Append(' ');
							lexer.Advance();
						}
						var meta_header_match = MetaHeaderRegexp.Match(meta_header.ToString());
						if(!meta_header_match.Success){
							AddError(ErrorCode.InvalidFileHeader, 1, 1, "Invalid Map file!");
							return null;
						}else{
							version_str = meta_header_match.Groups[1].Value;
						}
					}

					BVE5Language.Ast.Statement stmt = null;
					var stmts = new List<BVE5Language.Ast.Statement>();
					while(lexer.Current != Token.EOF){
						stmt = ParseStatement(lexer);
						if(enable_strict_parsing && !has_error_reported || !enable_strict_parsing)
							stmts.Add(stmt);
						
						if(has_error_reported)
							has_error_reported = false;
					}

					return AstNode.MakeSyntaxTree(stmts, fileName, version_str, BVE5FileKind.RouteFile, new TextLocation(1, 1), stmts.Last().EndLocation, Errors.ToList());
				}
			}
		}

		// let-statement | [\d]+ ';' | ident ['[' ident ']'] '.' ident '(' [args] ')' ';'
		BVE5Language.Ast.Statement ParseStatement(BVE5RouteFileLexer lexer)
		{
			Token token = lexer.Current;
			BVE5Language.Ast.Expression expr = null;

			switch(token.Kind){
			case TokenKind.IntegerLiteral:
				expr = ParseLiteral(lexer);
				break;
				
			case TokenKind.Identifier:
				expr = ParseIdent(lexer);
				if(lexer.Current.Literal == "[")
					expr = ParseIndexExpr(lexer, expr);

				token = lexer.Current;
				if(token.Literal != "."){
					AddError(ErrorCode.SyntaxError, token.Line, token.Column, "Expected '.' but got " + token.Literal);
					if(enable_strict_parsing)
						return null;
				}
				expr = ParseMemberRef(lexer, expr);
				expr = ParseInvokeExpr(lexer, expr);
				break;
				
			case TokenKind.KeywordToken:
				return ParseLetStatement(lexer);
				
			default:
				AddError(ErrorCode.SyntaxError, token.Line, token.Column, "A statement must start with a keyword, an integer literal or an identifier.");
				TryRecovery(lexer);
				return null;
			}

			token = lexer.Current;
			if(token.Literal != ";"){
				AddError(ErrorCode.SyntaxError, token.Line, token.Column, "Unexpected character: " + token.Literal);
				if(enable_strict_parsing)
					return null;
			}else{
				lexer.Advance();
			}
			
			return AstNode.MakeStatement(expr, expr.StartLocation, token.EndLoc);   //token should be pointing to a semicolon token
		}
		
		// "let" definition ";"
		LetStatement ParseLetStatement(BVE5RouteFileLexer lexer)
		{
			Token token = lexer.Current;
			Debug.Assert(token.Literal == "let", "Really meant a let statement?");
			if(token.Literal != "let"){
				AddError(ErrorCode.UnknownKeyword, token.Line, token.Column, "Unknown keyword " + token.Literal);
				if(enable_strict_parsing)
					return null;
			}else{
				lexer.Advance();
			}
			
			var expr = ParseDefinition(lexer);
			token = lexer.Current;
			if(token.Literal != ";"){
				AddError(ErrorCode.SyntaxError, token.Line, token.Column, "Unexpected character: " + token.Literal);
				if(enable_strict_parsing)
					return null;
			}else{
				lexer.Advance();
			}
			
			return AstNode.MakeLetStatement(expr, expr.StartLocation, token.EndLoc);
		}
		
		// ident '=' expr
		DefinitionExpression ParseDefinition(BVE5RouteFileLexer lexer)
		{
			Token token = lexer.Current;
			var start_loc = token.StartLoc;
			Debug.Assert(token.Kind == TokenKind.Identifier, "Really meant a definition?");
			
			var lhs = ParseIdent(lexer);
			token = lexer.Current;
			if(token.Literal != "="){
				AddError(ErrorCode.SyntaxError, token.Line, token.Column, "Expected '=' but got " + token.Literal);
				if(enable_strict_parsing)
					return null;
			}else{
				lexer.Advance();
			}
			
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
			Token token = lexer.Current;
			Debug.Assert(token.Literal == "[", "Really meant an index reference?");
			lexer.Advance();
			
			token = lexer.Current;
			if(token.Kind != TokenKind.Identifier && token.Kind != TokenKind.IntegerLiteral){
				AddError(ErrorCode.SyntaxError, token.Line, token.Column, "Unexpected token: " + token.Literal +
                    "; The operator '[]' can only take a string or an index as its argument.");
				if(enable_strict_parsing)
					return null;
            }

			LiteralExpression literal = ParseLiteral(lexer);
			token = lexer.Current;
			if(token.Literal != "]"){
				AddError(ErrorCode.SyntaxError, token.Line, token.Column, "Expected ']' but got " + token.Literal);
				if(enable_strict_parsing)
					return null;
			}else{
				lexer.Advance();
				token = lexer.Current;
			}
			
			return AstNode.MakeIndexExpr(target, literal, target.StartLocation, token.StartLoc);
		}

		// ident '.' ident
		MemberReferenceExpression ParseMemberRef(BVE5RouteFileLexer lexer, BVE5Language.Ast.Expression parent)
		{
			Token token = lexer.Current;
			Debug.Assert(token.Literal == ".", "Really meant a member reference?");
			lexer.Advance();
			
			token = lexer.Current;
			Identifier ident = null;
			if(token.Kind == TokenKind.Identifier){
				ident = ParseIdent(lexer);
			}else{
				AddError(ErrorCode.SyntaxError, token.Line, token.Column, "Unexpected token: " + token.Kind);
				if(enable_strict_parsing)
					return null;
			}
			
			token = lexer.Current;
			return AstNode.MakeMemRef(parent, ident, parent.StartLocation, token.StartLoc);
		}

		// expr '(' [arguments] ')'
		Expression ParseInvokeExpr(BVE5RouteFileLexer lexer, BVE5Language.Ast.Expression callTarget)
		{
			if(has_error_reported) return callTarget;
			
			Token token = lexer.Current;
			Debug.Assert(token.Literal == "(", "Really meant an invoke expression?");
			lexer.Advance();
			token = lexer.Current;
			var args = new List<BVE5Language.Ast.Expression>();

			while(token.Kind != TokenKind.EOF && token.Literal != ")"){
                args.Add(ParseRValueExpression(lexer));

				token = lexer.Current;
				if(token.Literal == ","){
					lexer.Advance();
					token = lexer.Current;
				}
			}

			if(token.Kind == TokenKind.EOF){
				AddError(ErrorCode.UnexpectedEOF, token.Line, token.Column, "Unexpected EOF!");
				if(enable_strict_parsing)
					return null;
			}else if(token.Literal != ")"){
				AddError(ErrorCode.SyntaxError, token.Line, token.Column, "Missing ')'");
				if(enable_strict_parsing)
					return null;
			}else{
				lexer.Advance();
				token = lexer.Current;
			}
			
			return AstNode.MakeInvoke(callTarget, args, callTarget.StartLocation, token.StartLoc);    //token should be pointing to a closing parenthesis token
		}

		// path-literal | identifier | time-literal | literal
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
                AddError(ErrorCode.SyntaxError, token.Line, token.Column,
                         "A right-hand-side expression must be an identifier, a file path, a literal or a time format literal!");
                return null;
            }
        }

        // path-literal
        LiteralExpression ParsePathLiteral(BVE5RouteFileLexer lexer)
        {
        	if(has_error_reported) return null;
        	
            Token token = lexer.Current;
            var start_loc = token.StartLoc;
            Debug.Assert(token.Kind == TokenKind.Identifier, "Really meant a path literal?");
            var sb = new StringBuilder();

            while(token.Literal != "," && token.Literal != ")"){
            	if(token.Kind == TokenKind.EOF){
            		AddError(ErrorCode.UnexpectedEOF, token.Line, token.Column, "Unexpected EOF!");
            		if(enable_strict_parsing)
            			return null;
            		else
            			break;
            	}
                sb.Append(token.Literal);
                lexer.Advance();
                token = lexer.Current;
            }
            return AstNode.MakeLiteral(sb.ToString(), start_loc, token.StartLoc);
        }

		// number | any-string
		LiteralExpression ParseLiteral(BVE5RouteFileLexer lexer)
		{
			if(has_error_reported) return null;
			
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
			if(has_error_reported) return null;
			
			Token token = lexer.Current;
			Debug.Assert(token.Kind == TokenKind.IntegerLiteral, "Really meant a time literal?");
			int[] nums = new int[3];
			Token start_token = lexer.Current;

			for(int i = 0; i < 3; ++i){
				if(token.Kind == TokenKind.EOF){
					AddError(ErrorCode.UnexpectedEOF, token.Line, token.Column, "Unexpected EOF!");
					if(enable_strict_parsing)
						return null;
					else
						break;
				}
				
				nums[i] = Convert.ToInt32(token.Literal);

				lexer.Advance();
				if(i == 2) break;
				
				token = lexer.Current;
				if(token.Kind == TokenKind.EOF){
					AddError(ErrorCode.UnexpectedEOF, token.Line, token.Column, "Unexpected EOF!");
					if(enable_strict_parsing)
						return null;
					else
						break;
				}else if(token.Literal != ":"){
					AddError(ErrorCode.SyntaxError, token.Line, token.Column, "Expected ':' but got " + token.Literal);
				}
				
				lexer.Advance();
				token = lexer.Current;
			}

			return AstNode.MakeTimeFormat(nums[0], nums[1], nums[2], start_token.StartLoc, token.StartLoc);
		}
		#endregion
	}
}

