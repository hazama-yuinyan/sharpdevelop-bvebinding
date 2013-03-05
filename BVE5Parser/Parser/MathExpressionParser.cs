/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/03/02
 * Time: 14:29
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Diagnostics;
using System.IO;
using BVE5Language.Ast;

namespace BVE5Language.Parser.Extension
{
	/// <summary>
	/// The parser for Math-like expressions.
	/// </summary>
	/*public class MathExpressionParser
	{
		static object parse_lock = new object();
		
		#region public surface
		/// <summary>
		/// Parses a string.
		/// </summary>
		/// <param name='programSrc'>
		/// A string containing expressions.
		/// </param>
		public Expression Parse(string programSrc)
		{
			lock(parse_lock){
				using(var reader = new StringReader(programSrc)){
					var lexer = new MathExpressionLexer(reader);
					
					return ParseExpression(lexer);
				}
			}
		}
		#endregion
		
		#region Implementation details
		Expression ParseExpression(MathExpressionLexer lexer)
		{
			
		}
		
		
		
		// unary-expression |  ('+' | '-') 
		BinaryExpression ParseAdditive(MathExpressionLexer lexer)
		{
			
		}
		
		// unary-expression | multiplicative ('*' | '/') additive
		BinaryExpression ParseMultiplicative(MathExpressionLexer lexer)
		{
			var expr = ParseUnary(lexer);
			Token token = lexer.Current;
			
			while(token.Literal == "*" || token.Literal == "/"){
				lexer.Advance();
				
			}
		}
		
		// primary | '-' unary-expression
		UnaryExpression ParseUnary(MathExpressionLexer lexer)
		{
			Token token = lexer.Current;
			if(token.Literal == "+" || token.Literal == "-"){
				lexer.Advance();
				var expr = ParseUnary(lexer);
				return AstNode.MakeUnary(expr, (token.Literal == "-") ? Operator.Minus : Operator.Plus, token.StartLoc, lexer.Current.StartLoc);
			}else{
				return ParsePrimary(lexer);
			}
		}
		
		// '(' expression ')' | number | identifier | index-reference
		Expression ParsePrimary(MathExpressionLexer lexer)
		{
			Token token = lexer.Current;
			var start_loc = token.StartLoc;
			Expression res = null;
			
			if(token.Literal == "("){
				lexer.Advance();
				res = ParseExpression(lexer);
				token = lexer.Current;
				if(token.Literal != ")")
					throw new BVE5ParserException(token.Line, token.Column, "Could not find the matching ')'");
				
				lexer.Advance();
			}else if(token.Literal == "$"){
				res = ParseIndexReference(lexer);
			}else if(token.Kind == TokenKind.IntegerLiteral || token.Kind == TokenKind.FloatLiteral){
				res = ParseLiteral(lexer);
			}else{
				throw new BVE5ParserException("Unknown token type!");
			}
			
			return res;
		}
		
		// expr {',' expr}
		/*SequenceExpression ParseSequence(MathExpressionLexer lexer)
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
		
		// '$' '{' integer '}'
		Identifier ParseIndexReference(MathExpressionLexer lexer)
		{
			Token token = lexer.Current;
			var start_loc = token.StartLoc;
			Debug.Assert(token.Literal == "$", "Really meant an index reference?");
			lexer.Advance();
			token = lexer.Current;
			if(token.Literal != "{")
				throw new BVE5ParserException(token.Line, token.Column, "Expected '{' but got " + token.Literal);
			
			lexer.Advance();
			token = lexer.Current;
			if(token.Kind != TokenKind.IntegerLiteral)
				throw new BVE5ParserException(token.Line, token.Column, "An index reference must be an integer.");
			
			lexer.Advance();
			token = lexer.Current;
			if(token.Literal != "}")
				throw new BVE5ParserException(token.Line, token.Column, "Expected '}' but got " + token.Literal);
			
			lexer.Advance();
			token = lexer.Current;
			return AstNode.MakeIdent(token.Literal, start_loc, token.StartLoc);
		}
		
		// anyCharacterExcept-'+'-'-'-'*'-'/'
		Identifier ParseIdent(MathExpressionLexer lexer)
		{
			Debug.Assert(lexer.Current.Kind == TokenKind.Identifier, "Really meant an identifier?");
			Token token = lexer.Current;
			lexer.Advance();
			return AstNode.MakeIdent(token.Literal, token.StartLoc, lexer.Current.StartLoc);
		}
		
		// number
		LiteralExpression ParseLiteral(MathExpressionLexer lexer)
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
	}*/
}
