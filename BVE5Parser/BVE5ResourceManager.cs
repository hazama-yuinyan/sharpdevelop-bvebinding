/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/02/21
 * Time: 14:08
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;

using Newtonsoft.Json;

namespace BVE5Language
{
	/// <summary>
	/// Description of BVE5ResourceManager.
	/// </summary>
	public static class BVE5ResourceManager
	{
		private static readonly string[] TypeNames;
		private static readonly Dictionary<string, string[]> MethodNames;
		
		class BuiltinsDefinition
		{
			public string[] TypeNames{get; set;}
			public Dictionary<string, string[]> Methods{get; set;}
		}
		
		public static T DeserializeObject<T>(XmlTextReader reader) where T : class
		{
			/*var target_type = typeof(T);
			while(reader.Read()){
				reader.LocalName
			}*/
			return (T)null;
		}
		
		private static object ReadObject(XmlTextReader reader)
		{
			return null;
		}
		
		static BVE5ResourceManager()
		{
			//TODO: implement it
			/*using(var s = typeof(BVE5ResourceManager).Assembly.GetManifestResourceStream("BVE5ResourceManager.BVE5BuiltinNames.xml")){
				using(var reader = new XmlTextReader(s)){
					
				}
			}*/
			var builtin_names = JsonConvert.DeserializeObject<BuiltinsDefinition>(File.ReadAllText("../../../resources/BVE5BuiltinNames.json"));
			TypeNames = builtin_names.TypeNames;
			MethodNames = builtin_names.Methods;
		}
		
		/// <summary>
        /// Tests whether the name is a builtin type name.
        /// </summary>
        /// <remarks>Case is insignificant when comparing names</remarks>
        /// <param name="name"></param>
        /// <returns>true, if it is; otherwise false</returns>
        public static bool IsBuiltinTypeName(string name)
        {
            return TypeNames.Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Tests whether the specified builtin type has the method.
        /// </summary>
        /// <remarks>Case is insignificant when comparing names</remarks>
        /// <param name="builtinTypeName"></param>
        /// <param name="methodName"></param>
        /// <returns>true, if it has; otherwise false</returns>
        public static bool BuiltinTypeHasMethod(string builtinTypeName, string methodName)
        {
            return MethodNames[builtinTypeName].Contains(methodName, StringComparer.OrdinalIgnoreCase);
        }
        
        public static string[] GetAllTypeNames()
        {
        	return TypeNames;
        }
	}
}
