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

namespace BVEBinding
{
	/// <summary>
	/// Description of BVE5LanguageBinding.
	/// </summary>
	public class BVE5LanguageBinding : DefaultLanguageBinding
	{
		public override IFormattingStrategy FormattingStrategy {
			get {
				throw new NotImplementedException();
			}
		}
		
		public override ICSharpCode.SharpDevelop.Dom.LanguageProperties Properties {
			get {
				throw new NotImplementedException();
			}
		}
		
		public override IBracketSearcher BracketSearcher {
			get {
				throw new NotImplementedException();
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
