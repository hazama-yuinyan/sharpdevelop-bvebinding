/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/03/07
 * Time: 13:30
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.TypeSystem;

namespace BVE5Language.Parser
{
	public abstract class AbstractMessage
	{
		readonly string[] extra_info;
		protected readonly int code;
		protected readonly TextLocation location;
		readonly string message;

		protected AbstractMessage(int code, TextLocation loc, string msg, List<string> extraInfo)
		{
			this.code = code;
			if(code < 0)
				this.code = 8000 - code;

			this.location = loc;
			this.message = msg;
			if(extraInfo != null && extraInfo.Count != 0)
				this.extra_info = extraInfo.ToArray();
		}

		protected AbstractMessage(AbstractMessage aMsg)
		{
			this.code = aMsg.code;
			this.location = aMsg.location;
			this.message = aMsg.message;
			this.extra_info = aMsg.extra_info;
		}

		public int Code {
			get { return code; }
		}

		public override bool Equals(object obj)
		{
			AbstractMessage msg = obj as AbstractMessage;
			if(msg == null)
				return false;

			return code == msg.code && location.Equals(msg.location) && message == msg.message;
		}

		public override int GetHashCode()
		{
			return code.GetHashCode();
		}

		public abstract bool IsWarning { get; }

		public TextLocation Location {
			get { return location; }
		}

		public abstract string MessageType { get; }

		public string[] RelatedSymbols {
			get { return extra_info; }
		}

		public string Text {
			get { return message; }
		}
	}

	sealed class WarningMessage : AbstractMessage
	{
		public WarningMessage(int code, TextLocation loc, string message, List<string> extraInfo)
			: base(code, loc, message, extraInfo)
		{
		}

		public override bool IsWarning {
			get { return true; }
		}

		public override string MessageType {
			get {
				return "warning";
			}
		}
	}

	sealed class ErrorMessage : AbstractMessage
	{
		public ErrorMessage(int code, TextLocation loc, string message, List<string> extraInfo)
			: base (code, loc, message, extraInfo)
		{
		}

		public ErrorMessage(AbstractMessage otherMsg)
			: base (otherMsg)
		{
		}

		public override bool IsWarning {
			get { return false; }
		}

		public override string MessageType {
			get {
				return "error";
			}
		}
	}

	//
	// Generic base for any message writer
	//
	public abstract class ReportPrinter
	{
		#region Properties

		public int ErrorsCount { get; protected set; }
		
		public int WarningsCount { get; private set; }
	
		//
		// When (symbols related to previous ...) can be used
		//
		public virtual bool HasRelatedSymbolSupport {
			get { return true; }
		}

		#endregion


		protected virtual string FormatText(string txt)
		{
			return txt;
		}

		public virtual void Print(AbstractMessage msg, bool showFullPath)
		{
			if(msg.IsWarning)
				++WarningsCount;
			else
				++ErrorsCount;
		}

		protected void Print(AbstractMessage msg, TextWriter output, bool showFullPath)
		{
			StringBuilder txt = new StringBuilder();
			if(!msg.Location.IsEmpty){
				if(showFullPath)
					txt.Append(msg.Location.ToString());
				else
					txt.Append(msg.Location.ToString());

				txt.Append(" ");
			}

			txt.AppendFormat("{0} BVE5{1:0000}: {2}", msg.MessageType, msg.Code, msg.Text);

			if(!msg.IsWarning)
				output.WriteLine(FormatText(txt.ToString()));
			else
				output.WriteLine(txt.ToString());

			if(msg.RelatedSymbols != null){
				foreach(string s in msg.RelatedSymbols)
					output.WriteLine(s + msg.MessageType + ")");
			}
		}

		public void Reset()
		{
			// HACK: Temporary hack for broken repl flow
			ErrorsCount = WarningsCount = 0;
		}
	}
	
	public class ErrorReportPrinter : ReportPrinter
	{
		readonly string file_name;
		public readonly List<Error> Errors = new List<Error>();
			
		public ErrorReportPrinter(string fileName)
		{
			this.file_name = fileName;
		}
			
		public override void Print(AbstractMessage msg, bool showFullPath = false)
		{
			base.Print(msg, showFullPath);
			var new_error = new Error(msg.IsWarning ? ErrorType.Warning : ErrorType.Error, msg.Text,
                                     new DomRegion(file_name, msg.Location.Line, msg.Location.Column));
			Errors.Add(new_error);
		}
	}
}
