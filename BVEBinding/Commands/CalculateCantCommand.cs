/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/03/03
 * Time: 22:24
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using BVE5Binding.Dialogs;
using ICSharpCode.Core;

namespace BVE5Binding.Commands
{
	/// <summary>
	/// Calculates the amount of cant in terms of physics.
	/// </summary>
	public sealed class CalculateCantCommand : AbstractCommand
	{
		public override void Run()
		{
			var dialog = new CalculateCantDialog();
			dialog.Show();
		}
	}
}
