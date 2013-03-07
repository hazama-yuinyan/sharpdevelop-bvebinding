/*
 * Created by SharpDevelop.
 * User: Ryouta
 * Date: 2013/03/03
 * Time: 18:54
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using ICSharpCode.SharpDevelop.Editor.CodeCompletion;

namespace BVE5Binding.Completion
{
	/// <summary>
	/// Description of CompletionDataHelper.
	/// </summary>
	internal static class CompletionDataHelper
	{
		internal static ICompletionItemList CreateListFromString(IEnumerable<string> data)
		{
			var list = new DefaultCompletionItemList();
			foreach(var a_data in data)
				list.Items.Add(new DefaultCompletionItem(a_data));
			
			list.SortItems();
			return list;
		}
	}
}
