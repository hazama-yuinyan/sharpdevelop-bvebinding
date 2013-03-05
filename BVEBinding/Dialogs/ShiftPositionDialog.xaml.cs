/*
 * Created by SharpDevelop.
 * User: Ryouta
 * Date: 2013/02/22
 * Time: 15:39
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
using BVEBinding.Commands;

namespace BVEBinding.Dialogs
{
	/// <summary>
	/// Interaction logic for ShiftPositionDialog.xaml
	/// </summary>
	public partial class ShiftPositionDialog : Window
	{
		public int AmountOfShift{
			get{
				return int.Parse(amount_of_shift.Text);
			}
		}
		public ShiftPositionDialog()
		{
			InitializeComponent();
		}
		
		void OkButtonClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}
		
		void CancelButtonClick(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}
	}
}