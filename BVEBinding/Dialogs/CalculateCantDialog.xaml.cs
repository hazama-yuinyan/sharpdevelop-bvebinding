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
			calculator = new CantCalculator(new IdealCantCalculateStrategy());
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
		
		void SpeedTextboxTextChanged(object sender, TextChangedEventArgs e)
		{
			uint speed;
			if(!uint.TryParse(speed_textbox.Text, out speed)){
				MessageBox.Show(string.Format(StringParser.Parse("${res:CalculateCantDialog.ErrorMsgNegativeInteger}"), "speed"), "Error",
				                MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
			
			calculator.Strategy.Speed = speed;
			result_textbox.Text = calculator.AttemptCalculation();
		}
		
		void RadiusTextboxTextChanged(object sender, TextChangedEventArgs e)
		{
			uint radius;
			if(!uint.TryParse(radius_textbox.Text, out radius)){
				MessageBox.Show(string.Format(StringParser.Parse("${res:CalculateCantDialog.ErrorMsgNegativeInteger}"), "radius"), "Error",
				                MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
			
			calculator.Strategy.CurveRadius = radius;
			result_textbox.Text = calculator.AttemptCalculation();
		}
		
		void GaugeTextboxTextChanged(object sender, TextChangedEventArgs e)
		{
			uint gauge;
			if(!uint.TryParse(gauge_textbox.Text, out gauge)){
				MessageBox.Show(string.Format(StringParser.Parse("${res:CalculateCantDialog.ErrorMsgNegativeInteger}"), "gauge"), "Error",
				                MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
			
			calculator.Strategy.SetGauge(gauge);
			result_textbox.Text = calculator.AttemptCalculation();
		}
	}
}