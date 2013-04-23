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
using BVE5Binding.Other;
using ICSharpCode.Core;

namespace BVE5Binding.Commands
{
	/// <summary>
	/// Calculates the equilibrium cant. That is, it calculates the cant where the passengers don't feel any lateral forces(the ones towards sideways).
	/// </summary>
	internal class EquilibriumCantCalculateStrategy : AbstractCalculateCantStrategy
	{
		internal override double Calculate()
		{
			if(speed == 0 || radius == 0 || double.IsNaN(gauge)) return double.NaN;
			double speed_in_double = (double)speed * 1000.0 / (60.0 * 60.0);	//speed_in_double is in the unit of meters per second
			double radius_in_double = (double)radius;							//radius_in_double is in the unit of meters
			double denom = Math.Sqrt(Math.Pow(speed_in_double, 4.0) + PhysicalConstants.GravitationalAcceleration * PhysicalConstants.GravitationalAcceleration *
			                         radius_in_double * radius_in_double);
			double nom = gauge * speed_in_double * speed_in_double;
			
			double cant = nom / denom;	//cant is in the unit of meters
			return cant;
		}
	}
}
