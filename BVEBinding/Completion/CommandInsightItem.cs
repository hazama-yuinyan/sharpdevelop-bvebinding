/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/03/10
 * Time: 20:26
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using ICSharpCode.SharpDevelop.Editor.CodeCompletion;

namespace BVE5Binding.Completion
{
	/// <summary>
	/// Represents an Insight window item for BVE5 commands.
	/// </summary>
	public class CommandInsightItem : IInsightItem
	{
		readonly string doc_string;
		string header_text;
		bool description_created;
		string description;
		int highlight_parameter;
		object pretty_header;
		
		public CommandInsightItem(string headerString, string docString)
		{
			if(headerString == null)
				throw new ArgumentNullException("headerString");
			
			if(docString == null)
				throw new ArgumentNullException("docString");
			
			header_text = headerString;
			doc_string = docString;
			highlight_parameter = -1;
		}
		
		public int HighlightParameter{
			get{return highlight_parameter;}
			set{
				if(value != highlight_parameter){
					highlight_parameter = value;
					pretty_header = GenerateHeader();
				}
			}
		}
		
		object GenerateHeader()
		{
			return header_text;
		}
		
		public object Header {
			get {
				if(pretty_header == null)
					pretty_header = GenerateHeader();
				
				return pretty_header;
			}
		}
		
		public object Content {
			get {
				if(!description_created){
					description = CompletionDataHelper.ConvertDocumentation(doc_string);
					description_created = true;
				}
				
				return description;
			}
		}
	}
}
