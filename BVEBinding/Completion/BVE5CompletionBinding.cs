/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/02/21
 * Time: 11:15
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Linq;
using BVE5Language;
using BVE5Language.Resolver;
using BVE5Language.TypeSystem;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Editor.CodeCompletion;
using ICSharpCode.NRefactory.Completion;
using ICSharpCode.SharpDevelop.Gui;

namespace BVE5Binding.Completion
{
	/// <summary>
	/// Code Completion binding for BVE5 files.
	/// </summary>
	public class BVE5CodeCompletionBinding : DefaultCodeCompletionBinding
	{
		ICodeCompleter completer;
		
		public BVE5CodeCompletionBinding()
		{
			LoggingService.Debug("Initializing BVE5CodeCompletionBinding...");
			WorkbenchSingleton.Workbench.ActiveViewContentChanged += OnActiveViewContentChanged;
			OnActiveViewContentChanged(this, null);
		}
		
		public override CodeCompletionKeyPressResult HandleKeyPress(ITextEditor editor, char ch)
		{
			if(completer == null) return CodeCompletionKeyPressResult.None;
			
			var result = completer.TryComplete(editor, ch, insightHandler);
			if(result.Item1)
				return result.Item2;
			else
				return base.HandleKeyPress(editor, ch);
		}
		
		public override bool CtrlSpace(ITextEditor editor)
		{
			throw new NotImplementedException();
		}
		
		public void OnActiveViewContentChanged(object sender, EventArgs e)
		{
			var provider = WorkbenchSingleton.Workbench.ActiveViewContent as ITextEditorProvider;
			if(provider == null){
				completer = null;
				insightHandler = null;
				return;
			}
			
			var first_line = provider.TextEditor.Document.GetLine(1);
			switch(first_line.Text.Trim()){
			case "BveTs Map 1.00":
				completer = new BVE5RouteFileCompleter();
				insightHandler = new BVE5RouteFileInsightWindowHandler((BVE5RouteFileCompleter)completer);
				break;
				
			case "BveTs Structure List 1.00":
				completer = new BVE5CommonFileCompleter(BVE5FileKind.StructureList);
				insightHandler = new BVE5CommonFileInsightWindowHandler(BVE5FileKind.StructureList, (BVE5CommonFileCompleter)completer);
				break;
				
			case "BveTs Station List 1.01":
				completer = new BVE5CommonFileCompleter(BVE5FileKind.StationList);
				insightHandler = new BVE5CommonFileInsightWindowHandler(BVE5FileKind.StationList, (BVE5CommonFileCompleter)completer);
				break;
				
			case "BveTs Signal Aspects List 1.00":
				completer = new BVE5CommonFileCompleter(BVE5FileKind.SignalAspectsList);
				insightHandler = new BVE5CommonFileInsightWindowHandler(BVE5FileKind.SignalAspectsList, (BVE5CommonFileCompleter)completer);
				break;
				
			case "BveTs Sound List 1.00":
				completer = new BVE5CommonFileCompleter(BVE5FileKind.SoundList);
				insightHandler = new BVE5CommonFileInsightWindowHandler(BVE5FileKind.SoundList, (BVE5CommonFileCompleter)completer);
				break;
				
			case "BveTs Train 0.01":
				completer = new BVE5InitFileCompleter(BVE5FileKind.TrainFile);
				insightHandler = null;//new BVE5RouteFileInsightWindowHandler(BVE5FileKind.Train);
				break;
				
			case "BveTs Vehicle Parameters 1.01":
				completer = new BVE5InitFileCompleter(BVE5FileKind.VehicleParametersFile);
				insightHandler = null;//new BVE5RouteFileInsightWindowHandler(BVE5FileKind.VehicleParameters);
				break;
				
			case "Version 1.0":
				completer = new BVE5InitFileCompleter(BVE5FileKind.InstrumentPanelFile);
				insightHandler = null;//new BVE5RouteFileInsightWindowHandler(BVE5FileKind.InstrumentPanel);
				break;
				
			case "Bvets Vehicle Sound 2.00":
				completer = new BVE5InitFileCompleter(BVE5FileKind.VehicleSoundFile);
				insightHandler = null;//new BVE5RouteFileInsightWindowHandler(BVE5FileKind.VehicleSound);
				break;
				
			default:
				completer = null;
				insightHandler = null;
				break;
			}
		}
	}
}
