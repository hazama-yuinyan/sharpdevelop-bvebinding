/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/03/09
 * Time: 16:23
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace BVE5Binding.Completion
{
	/// <summary>
	/// Represents the type of file.
	/// </summary>
	internal enum BVE5FileKind
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
}
