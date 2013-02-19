/*
 * Created by SharpDevelop.
 * User: Ryouta
 * Date: 2013/02/18
 * Time: 12:51
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;

namespace BVE5Language.TypeSystem
{
	/// <summary>
	/// Description of PrimitiveTypeCache.
	/// </summary>
	sealed class PrimitiveTypeCache
	{
		private readonly ICompilation compilation;
		private readonly IType[] primitive_types = new IType[BVEBuiltins.PrimitiveTypeCodeCount];
		
		public PrimitiveTypeCache(ICompilation compilation)
		{
			this.compilation = compilation;
		}
		
		public IType FindType(BVEPrimitiveTypeCode typeCode)
		{
			IType type = LazyInit.VolatileRead(ref primitive_types[(int)typeCode]);
			if(type != null)
				return type;
			
			return LazyInit.GetOrSet(ref primitive_types[(int)typeCode], SearchType(typeCode));
		}
		
		private IType SearchType(BVEPrimitiveTypeCode typeCode)
		{
			var type_ref = PrimitiveTypeReference.Get(typeCode);
			if(type_ref == null)
				return SpecialType.UnknownType;
			
			var type_name = new TopLevelTypeName(type_ref.Namespace, type_ref.Name, 0);
			foreach(var asm in compilation.Assemblies){
				var type_def = asm.GetTypeDefinition(type_name);
				if(type_def != null)
					return type_def;
			}
			return new UnknownType(type_name);
		}
	}
}
