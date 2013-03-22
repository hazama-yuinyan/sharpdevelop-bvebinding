/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/03/22
 * Time: 13:52
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Text.RegularExpressions;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Gui;

namespace BVE5Binding
{
	/// <summary>
	/// Tests the header of the currently opened document.
	/// </summary>
	/// <attribute name="headercontent">
	/// The content of the header. It can be specified as regular expression.
	/// </attribute>
	/// <example title="Test if any BVE5 map file is being edited">
	/// &lt;Condition name="BVE5HeaderVerifier" headercontent="BveTs Map\s*[\d.]+" ignore_case="true"&gt;
	/// </example>
	public class BVEHeaderConditionEvaluator : IConditionEvaluator
	{
		public bool IsValid(object owner, Condition condition)
		{
			string header_string = condition.Properties["headercontent"];
			string will_ignore_case_str = condition.Properties["ignore_case"] ?? "false";
			bool will_ignore_case = Convert.ToBoolean(will_ignore_case_str);
			var provider = WorkbenchSingleton.Workbench.ActiveViewContent as ITextEditorProvider;
	
			if(provider != null){
				var doc = provider.TextEditor.Document;
				var first_line_content = doc.GetLine(1).Text;
				return Regex.IsMatch(first_line_content, header_string, will_ignore_case ? RegexOptions.IgnoreCase : RegexOptions.None);
			}
			
			return false;
		}
	}
}
