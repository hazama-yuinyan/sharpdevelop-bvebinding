/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/03/09
 * Time: 16:23
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace BVE5Language.Ast
{
	/// <summary>
	/// Represents the type of file.
	/// </summary>
	public enum BVE5FileKind
	{
		RouteFile,
		StructureList,
		StationList,
		SignalAspectsList,
		SoundList,
		TrainFile,
		VehicleParametersFile,
		InstrumentPanelFile,
		VehicleSoundFile
	}
	
	public static class FileKindHelper
	{
		public static string GetTypeNameFromFileKind(BVE5FileKind kind)
		{
			switch(kind){
			case BVE5FileKind.RouteFile:
				return "Route";
				
			case BVE5FileKind.StructureList:
				return "Structure";
				
			case BVE5FileKind.StationList:
				return "Station";
				
			case BVE5FileKind.SignalAspectsList:
				return "Signal";
				
			case BVE5FileKind.SoundList:
				return "Sound";
				
			case BVE5FileKind.TrainFile:
				return "Train";
				
			case BVE5FileKind.VehicleParametersFile:
				return "VehicleParams";
				
			case BVE5FileKind.InstrumentPanelFile:
				return "Instrument";
				
			case BVE5FileKind.VehicleSoundFile:
				return "VehicleSound";
				
			default:
				throw new ArgumentException();
			}
		}
	}
}
