/*
 * Created by SharpDevelop.
 * User: Ryouta
 * Date: 2013/02/22
 * Time: 1:17
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Linq;
using ICSharpCode.Core;
using ICSharpCode.NRefactory;
using ICSharpCode.SharpDevelop;
using BVE5Language.Ast;
using BVE5Language.Parser;
using BVE5Language.Resolver;
using BVEBinding.Dialogs;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Gui;

namespace BVEBinding.Commands
{
	/// <summary>
	/// This class is responsible for shifting positions in all or parts of the position statements.
	/// Thus this class enables you to shift the whole or parts of route forward or backward.
	/// </summary>
	public sealed class ShiftPositionsCommand : AbstractMenuCommand
	{
		public override void Run()
		{
			var dialog = new ShiftPositionDialog();
			var result = dialog.ShowDialog();
			if(result.HasValue && result.Value)
				ShiftPositions(dialog.AmountOfShift);
		}
		
		void ShiftPositions(int amountShift)
		{
			//TODO: 数式入力による距離程シフトの対応・相対インデックスによる既存距離程の参照機能の追加
			var provider = WorkbenchSingleton.Workbench.ActiveViewContent as ITextEditorProvider;
			if(provider == null)
				return;
			
			var doc = provider.TextEditor.Document;
			int begin_line = 1;
			int end_line = doc.TotalNumberOfLines;
			
			if(provider.TextEditor.SelectionLength != 0){
				begin_line = doc.GetLineForOffset(provider.TextEditor.SelectionStart).LineNumber;
				end_line = doc.GetLineForOffset(provider.TextEditor.SelectionStart + provider.TextEditor.SelectionLength).LineNumber;
			}
			
			using(provider.TextEditor.Document.OpenUndoGroup()){	//do the real work
				var content_text = provider.TextEditor.Document.Text;
				var tree = new BVE5RouteFileParser().Parse(content_text, provider.TextEditor.FileName, true);
				var pos_stmts = tree.FindNodes(n => begin_line <= n.StartLocation.Line && n.EndLocation.Line <= end_line &&
				                                   n.Type == NodeType.Statement && ((Statement)n).Expr.Type == NodeType.Literal)
					.OfType<Statement>().ToArray();
				
				foreach(var stmt in pos_stmts){
					var literal_expr = (LiteralExpression)stmt.Expr;
					int original_value = (int)literal_expr.Value, modified_value = original_value + amountShift;
					literal_expr.ReplaceWith(new LiteralExpression(modified_value, literal_expr.StartLocation,
					                                               new TextLocation(literal_expr.EndLocation.Line,
					                                                                literal_expr.StartLocation.Column + modified_value.ToString().Length)));
					doc.SmartReplaceLine(doc.GetLine(stmt.StartLocation.Line), stmt.GetText());
				}
			}
		}
	}
}
