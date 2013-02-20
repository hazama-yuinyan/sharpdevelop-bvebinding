/*
 * Created by SharpDevelop.
 * User: Ryouta
 * Date: 2013/02/13
 * Time: 15:25
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Editor;

using BVEBinding.Formatting;

namespace BVEBinding
{
	/// <summary>
	/// Description of BVE5LanguageBinding.
	/// </summary>
	public class BVE5LanguageBinding : DefaultLanguageBinding
	{
		private IFormattingStrategy formatting_strategy = new BVE5FormattingStrategy();
		private IBracketSearcher bracket_searcher = new BVE5BracketSearcher();
		
		public override IFormattingStrategy FormattingStrategy {
			get {
				return formatting_strategy;
			}
		}
		
		public override ICSharpCode.SharpDevelop.Dom.LanguageProperties Properties {
			get {
				throw new NotImplementedException();
			}
		}
		
		public override IBracketSearcher BracketSearcher {
			get {
				return bracket_searcher;
			}
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
