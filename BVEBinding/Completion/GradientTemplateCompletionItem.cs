/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/04/17
 * Time: 12:56
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
	/// Completion item for the gradient template.
	/// </summary>
	public class GradientTemplateCompletionItem : ICompletionItem
	{
		readonly string TemplateText;
		GradientTemplateDialog dialog;
		event RoutedEventHandler handler;
		
		public GradientTemplateCompletionItem(ITextEditor editor)
		{
			TemplateText = StringParser.Parse("${res:Template.Text.Gradient}");
			dialog = new GradientTemplateDialog(editor.GetService(typeof(TextArea)) as TextArea);
		}
		
		public string Text {
			get {
				return "GradientTemplate";
			}
		}
		
		public string Description {
			get {
				return StringParser.Parse("${res:Template.Description.Gradient}");
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
				var text = dialog.GenerateText(TemplateText);
				context.Editor.Document.Replace(context.StartOffset, context.Length, text);
				context.EndOffset = context.StartOffset + text.Length;
				dialog.Close();
			};
			
			dialog.InsertButton.Click += handler;
			dialog.Show();
		}
	}
}
