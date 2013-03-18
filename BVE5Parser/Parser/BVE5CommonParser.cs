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
using System.Linq;
using System.Text.RegularExpressions;
using BVE5Language.Ast;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.TypeSystem;

namespace BVE5Language.Parser
{
	/// <summary>
	/// Parser for BVE5 common files.
	/// </summary>
	public class BVE5CommonParser
	{
		static object parse_lock = new object();
		
		readonly string FileKindName;
		readonly Regex MetaHeaderRegexp;
		
		ErrorReportPrinter error_report_printer = new ErrorReportPrinter(null);
		bool has_error_reported = false, enable_strict_parsing = false;

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
		/// Initializes a new instance of <see cref="BVE5Language.Parser.CommonParser"/>.
		/// </summary>
		/// <param name="headerString">The header text that will be verified if parseHeader option is specified.</param>
		/// <param name="fileKindName">The file type name of which the file is. This will be used for displaying type-specific errors.</param>
		public BVE5CommonParser(string headerString, string fileKindName)
		{
			MetaHeaderRegexp = new Regex(headerString + @"\s+([\d.]+)", RegexOptions.Compiled);
			FileKindName = fileKindName;
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
		public SyntaxTree Parse(string programSrc, string fileName = "")
		{
			enable_strict_parsing = true;
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
					var lexer = new BVE5CommonLexer(reader);
					lexer.Advance();
					
					string version_str = "unknown";
					if(parseHeader){
						var token = lexer.Current;
						lexer.Advance();
						
						var meta_header_match = MetaHeaderRegexp.Match(token.Literal);
						if(!meta_header_match.Success)
							AddError(ErrorCode.InvalidFileHeader, 1, 1, "Invalid " + FileKindName + " file!");
						else
							version_str = meta_header_match.Groups[1].Value;
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

					return AstNode.MakeSyntaxTree(stmts, fileName, version_str, new TextLocation(1, 1), stmts.Last().EndLocation);
				}
			}
		}
		
		// sequence '\n'
		Statement ParseStatement(BVE5CommonLexer lexer)
		{
			var command_invoke = ParseCommandInvoke(lexer);
			Token token = lexer.Current;
			if(token.Kind != TokenKind.EOL){
				AddError(ErrorCode.SyntaxError, token.Line, token.Column, "Expected EOL but got " + token.Literal + ".");
				if(enable_strict_parsing)
					return null;
			}else{
				lexer.Advance();
			}
			return AstNode.MakeStatement(command_invoke, command_invoke.StartLocation, token.EndLoc);
		}
		
		// argument {',' argument}
		InvocationExpression ParseCommandInvoke(BVE5CommonLexer lexer)
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
			
			return AstNode.MakeInvoke(AstNode.MakeIdent(FileKindName, start_loc, start_loc), children, start_loc, token.StartLoc);
		}
		
		// literal
		Expression ParseArgument(BVE5CommonLexer lexer)
		{
			if(has_error_reported) return null;
			
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
				AddError(ErrorCode.SyntaxError, token.Line, token.Column, "An argument must be a string, a literal or a time format literal!");
				return null;
			}
		}
		
		// any-string(including file path)
		LiteralExpression ParseString(BVE5CommonLexer lexer)
		{
			Token token = lexer.Current;
			Debug.Assert(token.Kind == TokenKind.StringLiteral, "Really meant a string?");
			lexer.Advance();
			return AstNode.MakeLiteral(token.Literal, token.StartLoc, lexer.Current.StartLoc);
		}
		
		// number
		LiteralExpression ParseLiteral(BVE5CommonLexer lexer)
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
		TimeFormatLiteral ParseTimeLiteral(BVE5CommonLexer lexer)
		{
			Token token = lexer.Current;
			Debug.Assert(token.Kind == TokenKind.IntegerLiteral, "Really meant a time literal?");
			int[] nums = new int[3];
			Token start_token = lexer.Current;

			for(int i = 0; i < 3; ++i){
				if(token.Kind == TokenKind.EOF)
					AddError(ErrorCode.UnexpectedEOF, token.Line, token.Column, "Unexpected EOF!");

				nums[i] = Convert.ToInt32(token.Literal);

				lexer.Advance();
				if(i == 2) break;
				
				token = lexer.Current;
				if(token.Kind == TokenKind.EOF)
					AddError(ErrorCode.UnexpectedEOF, token.Line, token.Column, "Unexpected EOF!");
				else if(token.Literal != ":")
					AddError(ErrorCode.SyntaxError, token.Line, token.Column, "Expected ':' but got " + token.Literal);

				lexer.Advance();
				token = lexer.Current;
			}

			return AstNode.MakeTimeFormat(nums[0], nums[1], nums[2], start_token.StartLoc, token.StartLoc);
		}
		#endregion
	}
}
