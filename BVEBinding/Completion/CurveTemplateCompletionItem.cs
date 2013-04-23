/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/04/10
 * Time: 16:13
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Windows;
using BVE5Binding.Dialogs;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Editor.CodeCompletion;

namespace BVE5Binding.Completion
{
	/// <summary>
	/// Completion item for the curve template.
	/// </summary>
	public class CurveTemplateCompletionItem : ICompletionItem
	{
		readonly string templateText;
		CurveTemplateDialog dialog;
		event RoutedEventHandler handler;
		
		public CurveTemplateCompletionItem(ITextEditor editor)
		{
			templateText = StringParser.Parse("${res:Template.Text.Curve}");
			dialog = new CurveTemplateDialog(editor.GetService(typeof(TextArea)) as TextArea);
		}
		
		public string Text {
			get {
				return "CurveTemplate";
			}
		}
		
		public string Description {
			get {
				return StringParser.Parse("${res:Template.Description.Curve}");
			}
		}
		
		public ICSharpCode.SharpDevelop.IImage Image {
			get {
				return ClassBrowserIconService.CodeTemplate;
			}
		}
		
		public double Priority {
			get {
				return 1.0;
			}
		}
		
		public void Complete(CompletionContext context)
		{
			if(handler != null)
				dialog.InsertButton.Click -= handler;
			
			handler = delegate(object sender, RoutedEventArgs e){
				var text = dialog.GenerateText(templateText);
				context.Editor.Document.Replace(context.StartOffset, context.Length, text);
				context.EndOffset = context.StartOffset + text.Length;
				dialog.Close();
			};
			
			dialog.InsertButton.Click += handler;
			dialog.Show();
			dialog.CurvePositionTextBox.Focus();
		}
	}
}
