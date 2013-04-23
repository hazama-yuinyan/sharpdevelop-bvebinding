/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 03/04/2013
 * Time: 13:42
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Text;
using ICSharpCode.Core;

namespace BVE5Binding.Commands
{
	/// <summary>
	/// The cant calculator.
	/// </summary>
	public class CantCalculator
	{
		const double HeightCenterOfMass = 1.65;
		const double MaxCantStandard = 0.180;
		const double MaxCantNarrow = 0.105;
		
		internal AbstractCalculateCantStrategy Strategy{
			get; set;
		}
		
		/// <summary>
		/// Gets the cant calculated in meters.
		/// </summary>
		public double Cant{
			get; private set;
		}
		
		internal CantCalculator(AbstractCalculateCantStrategy strategy)
		{
			Strategy = strategy;
		}
		
		/// <summary>
		/// Attempt to calculate the cant. If not enough information is given, it returns an empty string.
		/// </summary>
		/// <returns></returns>
		public string AttemptCalculation()
		{
			var cant = Strategy.Calculate();
			Cant = cant;
			if(double.IsNaN(cant))
				return "";
			
			string resource_name_suitable = IsSuitable(cant, Strategy.Gauge) ? "${res:CalculateCantDialog.TextSatisfied}" :
				"${res:CalculateCantDialog.TextNotSatisfied}";
			string text_suitable = StringParser.Parse(resource_name_suitable);
			
			double max_cant = (Strategy.GaugeAsInt == 1067) ? MaxCantNarrow :
				(Strategy.GaugeAsInt == 1435) ? MaxCantStandard : double.NaN;
			string resource_name_allowed = (double.IsNaN(max_cant)) ? "${res:CommonStrings.TextUndefined}" :
				(cant < max_cant) ? "${res:CommonStrings.TextYes}" : "${res:CommonStrings.TextNo}";
			string text_allowed = StringParser.Parse(resource_name_allowed);
			
			var result_tmpl = StringParser.Parse("${res:CalculateCantDialog.ResultTemplate}");
			string text_result = string.Format(result_tmpl, cant, text_suitable, text_allowed, Strategy.GaugeAsInt);
			return text_result;
		}
		
		/// <summary>
		/// Determines whether the cant calculated is suitable.
		/// </summary>
		/// <remarks>
		/// この結果の式は、h * tan(theta) &lt;= d / 6(ここでhは重心までの高さ、dは軌間)という式から導かれたものである。
		/// 具体的には、まず、シータは極小なのでc / d(cはカントの値)と近似でき、さらに両辺にdをかけることで導出できる。
		/// </remarks>
		/// <param name="cant">The value of cant</param>
		/// <returns>true, if it satisfies the condition; otherwise false</returns>
		internal static bool IsSuitable(double cant, double gauge)
		{
			//TODO: Rewrite the "remarks" section in English
			return HeightCenterOfMass * cant <= gauge * gauge / 6.0;
		}
	}
	
	internal abstract class AbstractCalculateCantStrategy
	{
		protected uint radius, speed, gauge_in_int;
		protected double gauge;
		
		/// <summary>
		/// Sets the radius of the curve in meters
		/// </summary>
		internal uint CurveRadius{
			set{
				radius = value;
			}
		}
		
		/// <summary>
		/// Sets the speed in kilometers per hour.
		/// </summary>
		internal uint Speed{
			set{
				speed = value;
			}
		}
		
		/// <summary>
		/// Sets the gauge in meters.
		/// </summary>
		internal double Gauge{
			get{
				return gauge;
			}
			set{
				gauge = value / 1000.0;
			}
		}
		
		/// <summary>
		/// Gets the gauge in millimeters.
		/// </summary>
		internal uint GaugeAsInt{
			get{
				return gauge_in_int;
			}
		}
		
		internal AbstractCalculateCantStrategy()
		{
			gauge = double.NaN;
		}
		
		internal void SetGauge(uint newValue)
		{
			gauge_in_int = newValue;
			Gauge = newValue;
		}
		
		internal abstract double Calculate();
	}
}
