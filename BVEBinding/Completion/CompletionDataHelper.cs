/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/03/03
 * Time: 18:54
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using ICSharpCode.Core;
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
		
		internal static string ConvertDocumentation(string docString)
		{
			var xml_reader = new XmlTextReader(new StringReader("<docroot>" + docString + "</docroot>"));
			var ret = new StringBuilder();
			try{
				xml_reader.Read();
				do{
					if(xml_reader.NodeType == XmlNodeType.Element){
						string elem_name = xml_reader.Name.ToLowerInvariant();
						switch(elem_name){
						case "param":
							ret.Append(Environment.NewLine);
							ret.Append(xml_reader["name"].Trim());
							ret.Append(": ");
							break;
							
						case "unit":
							ret.Append(Environment.NewLine);
							ret.Append("In the unit of ");
							break;
							
						case "default":
							ret.Append(Environment.NewLine);
							ret.Append("Default: ");
							break;
							
						case "explanation":
							ret.Append(Environment.NewLine);
							break;
						}
					}else if(xml_reader.NodeType == XmlNodeType.Text){
						ret.Append(xml_reader.Value);
					}
				}while(xml_reader.Read());
			}
			catch(Exception ex){
				LoggingService.Debug("Invalid documentation: " + ex.Message);
				return docString;
			}
			return ret.ToString();
		}
	}
}
