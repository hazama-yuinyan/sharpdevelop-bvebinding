/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/03/26
 * Time: 13:26
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using ICSharpCode.SharpDevelop.Editor;

namespace BVE5Binding.Tooltips
{
	/// <summary>
	/// Description of BVE5TooltipProvider.
	/// </summary>
	public class BVE5TooltipProvider : ITextAreaToolTipProvider
	{
		public void HandleToolTipRequest(ToolTipRequestEventArgs e)
		{
			if(!e.InDocument)
				return;
			
			
		}
	}
}
