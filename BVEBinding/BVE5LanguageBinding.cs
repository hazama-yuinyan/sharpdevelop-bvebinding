/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/02/13
 * Time: 15:25
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using BVE5Binding.Completion;
using BVE5Language.TypeSystem;
using BVE5Language.Ast;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Editor;
using BVE5Binding.Formatting;

namespace BVE5Binding
{
	/// <summary>
	/// Language binding for BVE5 files.
	/// </summary>
	public class BVE5LanguageBinding : DefaultLanguageBinding
	{
		static BVE5ProjectContent project;
		static BVE5Compilation compilation;
		IFormattingStrategy formatting_strategy = new BVE5FormattingStrategy();
		IBracketSearcher bracket_searcher = new BVE5BracketSearcher();
		
		public override IFormattingStrategy FormattingStrategy {
			get {
				return formatting_strategy;
			}
		}
		
		public override ICSharpCode.SharpDevelop.Dom.LanguageProperties Properties {
			get {
				LoggingService.Info("Language properties requested");
				throw new NotImplementedException();
			}
		}
		
		public override IBracketSearcher BracketSearcher {
			get {
				return bracket_searcher;
			}
		}
		
		/// <summary>
		/// Gets the unique BVE5ProjectContent instance per a program execution.
		/// </summary>
		static internal BVE5ProjectContent ProjectContent{
			get{return project;}
		}
		
		/// <summary>
		/// Gets the unique BVE5Compilation instance per a program execution.
		/// </summary>
		static internal BVE5Compilation Compilation{
			get{return compilation;}
		}
		
		static BVE5LanguageBinding()
		{
			project = new BVE5ProjectContent().AddAssemblyReferences(BVEBuiltins.GetBuiltinAssembly()) as BVE5ProjectContent;
			compilation = project.CreateCompilation() as BVE5Compilation;
		}
		
		internal static BVE5FileKind GetFileKindFromTypeName(string typeName)
		{
			switch(typeName){
			case "Structure":
				return BVE5FileKind.StructureList;
				
			case "Station":
				return BVE5FileKind.StationList;
				
			case "Signal":
				return BVE5FileKind.SignalAspectsList;
				
			case "Sound":
			case "Sound3D":
				return BVE5FileKind.SoundList;
				
			case "Train":
				return BVE5FileKind.TrainFile;
				
			default:
				throw new ArgumentException("Unknown type name!");
			}
		}
		
		public BVE5LanguageBinding()
		{
			ResourceService.RegisterStrings("BVE5Binding.Resources.StringResources", GetType().Assembly);
		}
		
		public override void Attach(ITextEditor editor)
		{
			base.Attach(editor);
		}
		
		public override void Detach()
		{
			base.Detach();
		}
	}
}
