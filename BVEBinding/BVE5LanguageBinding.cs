/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/02/13
 * Time: 15:25
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Editor;
using BVE5Binding.Formatting;

namespace BVE5Binding
{
	/// <summary>
	/// Description of BVE5LanguageBinding.
	/// </summary>
	public class BVE5LanguageBinding : DefaultLanguageBinding
	{
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
