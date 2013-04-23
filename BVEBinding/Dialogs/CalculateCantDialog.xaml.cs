/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/03/04
 * Time: 1:02
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using BVE5Binding.Commands;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Gui;

namespace BVE5Binding.Dialogs
{
	/// <summary>
	/// Interaction logic for CalculateCantDialog.xaml
	/// </summary>
	public partial class CalculateCantDialog : Window
	{
		readonly CantCalculator calculator;
		
		public CalculateCantDialog()
		{
			InitializeComponent();
			calculator = new CantCalculator(new EquilibriumCantCalculateStrategy());
		}
		
		void InsertButtonClick(object sender, RoutedEventArgs e)
		{
			var provider = WorkbenchSingleton.Workbench.ActiveViewContent as ITextEditorProvider;
			if(provider == null)
				return;
			
			var doc = provider.TextEditor.Document;
			doc.StartUndoableAction();
			doc.Insert(provider.TextEditor.Caret.Offset, calculator.Cant.ToString());
			doc.EndUndoableAction();
		}
		
		void CloseButtonClick(object sender, RoutedEventArgs e)
		{
			Close();
		}
		
		void TextBox_TextChaned(object sender, TextChangedEventArgs e)
		{
			var text_box = (TextBox)sender;
			string prop_name = text_box.Name.Substring(0, text_box.Name.Length - 7);
			uint val;
			if(!uint.TryParse(text_box.Text, out val)){
				MessageBox.Show(string.Format(StringParser.Parse("${res:CalculateCantDialog.ErrorMsgNegativeInteger}"), prop_name), "Error",
				                MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
			
			switch(prop_name){
			case "Speed":
				calculator.Strategy.Speed = val;
				break;
				
			case "Radius":
				calculator.Strategy.CurveRadius = val;
				break;
				
			case "Gauge":
				calculator.Strategy.SetGauge(val);
				break;
			}
			
			ResultTextbox.Text = calculator.AttemptCalculation();
		}
	}
}