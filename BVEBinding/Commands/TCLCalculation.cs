/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/04/21
 * Time: 16:46
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace BVE5Binding.Commands
{
	/// <summary>
	/// Abstract Calculation strategy for TCL.
	/// </summary>
	internal abstract class AbstractTCLCalculateStrategy
	{
		protected int radius, speed, gauge;
		protected double cant;
		
		internal int Radius{
			set{
				radius = value;
			}
		}
		
		internal int Speed{
			set{
				speed = value;
			}
		}
		
		internal int Gauge{
			set{
				gauge = value + 65;
			}
		}
		
		internal double GaugeInMeters{
			get{
				return (double)gauge / 1000.0;
			}
		}
		
		internal double Cant{
			set{
				cant = value;
			}
		}
		
		internal double CantInMeters{
			get{
				return cant / 1000.0;
			}
		}
		
		internal abstract double Calculate();
	}
}
