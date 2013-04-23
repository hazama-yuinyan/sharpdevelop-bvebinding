/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/04/21
 * Time: 17:44
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using BVE5Binding.Other;

namespace BVE5Binding.Commands
{
	/// <summary>
	/// Description of SineTCLCalculateStrategy.
	/// </summary>
	internal class SineTCLCalculateStrategy : AbstractTCLCalculateStrategy
	{
		internal override double Calculate()
		{
			var nom = 5 * Math.PI * (PhysicalConstants.GravitationalAcceleration * CantInMeters * radius + GaugeInMeters * speed * speed);
			var denom = 4 * GaugeInMeters * radius;
			return nom / denom;
		}
	}
}
