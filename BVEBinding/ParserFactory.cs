/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/03/11
 * Time: 1:57
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using BVE5Binding.Completion;
using BVE5Language.Parser;

namespace BVE5Binding
{
	/// <summary>
	/// Description of ParserFactory.
	/// </summary>
	internal static class ParserFactory
	{
		internal static BVE5RouteFileParser CreateRouteParser()
		{
			return new BVE5RouteFileParser();
		}
		
		internal static BVE5CommonParser CreateCommonParser(BVE5FileKind kind)
		{
			var header_str = (kind == BVE5FileKind.StructureList) ? "BveTs Structure List" :
				(kind == BVE5FileKind.StationList) ? "BveTs Station List" :
				(kind == BVE5FileKind.SignalAspectsList) ? "BveTs Signal Aspects List" :
				(kind == BVE5FileKind.SoundList) ? "BveTs Sound List" : null;
			return new BVE5CommonParser(header_str, kind.ToString());
		}
		
		internal static InitFileParser CreateInitFileParser(BVE5FileKind kind)
		{
			var header_str = (kind == BVE5FileKind.TrainFile) ? "BveTs Train" :
				(kind == BVE5FileKind.VehicleParametersFile) ? "BveTs Vehicle Parameters" :
				(kind == BVE5FileKind.InstrumentPanelFile) ? "Version" :
				(kind == BVE5FileKind.VehicleSoundFile) ? "Bvets Vehicle Sound" : null;
			return new InitFileParser(header_str, kind.ToString());
		}
	}
}
