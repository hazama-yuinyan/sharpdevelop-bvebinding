/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/03/11
 * Time: 1:14
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace BVE5Language.Parser
{
	/// <summary>
	/// Description of ErrorCode.
	/// </summary>
	public enum ErrorCode
	{
		InvalidFileHeader,
		SyntaxError,
		UnknownKeyword,
		UnexpectedToken,
		UnexpectedEOF,
		Other
	}
}
