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
using System.Text.RegularExpressions;
using BVE5Language.Ast;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.TypeSystem;

namespace BVE5Language.Parser
{
	/// <summary>
	/// Parser for BVE5 init files.
	/// </summary>
	public class InitFileParser
	{
		static object parse_lock = new object();
		
		readonly string FileTypeName;
		readonly Regex MetaHeaderRegexp;
		
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
		
		/// <summary>
		/// Initializes a new instance of <see cref="BVE5Language.Parser.InitFileParser"/>.
		/// </summary>
		/// <param name="headerString">The header text that will be verified if parseHeader option is specified.</param>
		/// <param name="fileTypeName">The file type name which the parser is supposed to parse. This will be used for displaying type-specific errors.</param>
		public InitFileParser(string headerString, string fileTypeName)
		{
			MetaHeaderRegexp = new Regex(headerString + @"\s*([\d.]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
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
		/// Whether it should parse the meta header.
		/// </param>
		public SyntaxTree Parse(string programSrc, string fileName = "", bool parseHeader = false)
		{
			enable_strict_parsing = true;
			return ParseImpl(programSrc.Replace(Environment.NewLine, "\n"), fileName, parseHeader);
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
		
		#region Implementation details
		SyntaxTree ParseImpl(string src, string fileName, bool parseHeader)
		{
			lock(parse_lock){
				if(!src.EndsWith("\n"))
					src += "\n";
				
				using(var reader = new StringReader(src)){
					var lexer = new InitFileLexer(reader);
					lexer.Advance();
					
					string version_str = "unknown";
					if(parseHeader){
						var meta_header = new StringBuilder();
						while(lexer.Current != Token.EOF && lexer.Current.Kind != TokenKind.EOL){
							meta_header.Append(lexer.Current.Literal);
							meta_header.Append(' ');
							lexer.Advance();
						}
						var meta_header_match = MetaHeaderRegexp.Match(meta_header.ToString());
						if(!meta_header_match.Success){
							AddError(ErrorCode.InvalidFileHeader, 1, 1, "Invalid " + FileTypeName + " file!");
							return null;
						}else{
							version_str = meta_header_match.Groups[1].Value;
						}
					}
					if(lexer.Current.Kind == TokenKind.EOL)
						lexer.Advance();

					BVE5Language.Ast.Statement stmt = null;
					var stmts = new List<BVE5Language.Ast.Statement>();
					while(lexer.Current != Token.EOF){
						stmt = ParseStatement(lexer);
						if(enable_strict_parsing && !has_error_reported || !enable_strict_parsing)
							stmts.Add(stmt);
						
						if(has_error_reported)
							has_error_reported = false;
					}

					return AstNode.MakeSyntaxTree(stmts, fileName, version_str, new TextLocation(1, 1), stmts.Last().EndLocation, Errors.ToList());
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
				AddError(ErrorCode.SyntaxError, token.Line, token.Column,
					     "A statement must start with an identifier or the sign '['.");
				while(token.Kind != TokenKind.EOL){
					if(token.Kind == TokenKind.EOF){
						AddError(ErrorCode.UnexpectedEOF, token.Line, token.Column, "Unexpected EOF!");
						return null;
					}
					lexer.Advance();
					token = lexer.Current;
				}
				lexer.Advance();
				return null;
			}
			
			token = lexer.Current;
			if(token.Kind != TokenKind.EOL){
				AddError(ErrorCode.SyntaxError, token.Line, token.Column, "Expected EOL but got " + token.Literal + ".");
				if(enable_strict_parsing)
					return null;
			}else{
				lexer.Advance();
			}
			
			token = lexer.Current;
			return AstNode.MakeStatement(expr, expr.StartLocation, token.StartLoc);
		}
		
		// '[' identifier ']' '\n'
		SectionStatement ParseSectionStatement(InitFileLexer lexer)
		{
			if(has_error_reported) return null;
			
			Token token = lexer.Current;
			var start_loc = token.StartLoc;
			Debug.Assert(token.Literal == "[", "Really meant a section statement?");
			
			lexer.Advance();
			Identifier ident = ParseIdent(lexer);
			token = lexer.Current;
			if(token.Literal != "]"){
				AddError(ErrorCode.SyntaxError, token.Line, token.Column, "Expected ']' but got " + token.Literal + ".");
				if(enable_strict_parsing)
					return null;
			}else{
				lexer.Advance();
			}
			
			token = lexer.Current;
			if(token.Kind != TokenKind.EOL){
				AddError(ErrorCode.SyntaxError, token.Line, token.Column, "Expected EOL but got " + token.Literal + ".");
				if(enable_strict_parsing)
					return null;
			}else{
				lexer.Advance();
			}
			
			token = lexer.Current;
			return AstNode.MakeSectionStatement(ident, start_loc, token.StartLoc);
		}
		
		// identifier '=' sequence
		DefinitionExpression ParseDefinition(InitFileLexer lexer)
		{
			if(has_error_reported) return null;
			
			Identifier lhs = ParseIdent(lexer);
			Token token = lexer.Current;
			if(token.Literal != "="){
				AddError(ErrorCode.SyntaxError, token.Line, token.Column, "Expected '=' but got " + token.Literal + ".");
				if(enable_strict_parsing)
					return null;
			}else{
				lexer.Advance();
			}
			
			SequenceExpression rhs = ParseSequence(lexer);
			Expression expr = rhs;
			if(rhs.Expressions.Count() == 1){		//Here, we'll simplify a list of one expression to a solo expression.
				var tmp = rhs.Expressions.First();	//This is needed because some definitions have more than one right-hand-side expressions.
				tmp.Remove();
				expr = tmp;
			}
			return AstNode.MakeDefinition(lhs, expr, lhs.StartLocation, lexer.Current.StartLoc);
		}
		
		// expr {',' expr}
		SequenceExpression ParseSequence(InitFileLexer lexer)
		{
			if(has_error_reported) return null;
			
			Token token = lexer.Current;
			var start_loc = token.StartLoc;
			Expression expr = null;
			var exprs = new List<Expression>();
			while(token.Kind != TokenKind.EOL){
				switch(token.Kind){
				case TokenKind.Identifier:
					if(token.Literal.Contains("."))
						expr = ParsePathLiteral(lexer);
					else if(token.Literal.StartsWith("#"))
						expr = ParseColorLiteral(lexer);
					else
						expr = ParseLiteral(lexer);
					
					break;
					
				case TokenKind.IntegerLiteral:
				case TokenKind.FloatLiteral:
					expr = ParseLiteral(lexer);
					break;
					
				default:
					AddError(ErrorCode.Other, token.Line, 1, string.Format("Line {0}: Invalid definition!", token.Line));
					break;
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
        
        // '#' {number}{3,6}
        LiteralExpression ParseColorLiteral(InitFileLexer lexer)
        {
        	Token token = lexer.Current;
        	Debug.Assert(token.Literal.StartsWith("#"), "Really meant a color literal?");
        	lexer.Advance();
        	return AstNode.MakeLiteral(token.Literal, token.StartLoc, lexer.Current.StartLoc);
        }

		// number | any-string
		LiteralExpression ParseLiteral(InitFileLexer lexer)
		{
			Token token = lexer.Current;
			Debug.Assert(token.Kind == TokenKind.IntegerLiteral || token.Kind == TokenKind.FloatLiteral || token.Kind == TokenKind.Identifier, "Really meant a literal?");
			lexer.Advance();
			if(token.Kind == TokenKind.FloatLiteral)
				return AstNode.MakeLiteral(Convert.ToDouble(token.Literal), token.StartLoc, token.EndLoc);
			else if(token.Kind == TokenKind.IntegerLiteral)
				return AstNode.MakeLiteral(Convert.ToInt32(token.Literal), token.StartLoc, token.EndLoc);
			else
				return AstNode.MakeLiteral(token.Literal, token.StartLoc, token.EndLoc);
		}
		#endregion
	}
}
