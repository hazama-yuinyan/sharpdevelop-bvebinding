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
using System.Linq.Expressions;
using System.IO;
using System.Reflection;
using System.Xml;
using Newtonsoft.Json;

namespace BVE5Language
{
	/// <summary>
	/// Description of BVE5ResourceManager.
	/// </summary>
	public static class BVE5ResourceManager
	{
		static readonly string[] TypeNames;
		static readonly Dictionary<string, string[]> MethodNames;
		
		class BuiltinsDefinition
		{
			public string[] TypeNames{get; set;}
			public Dictionary<string, string[]> Methods{get; set;}
		}
		
		/*public static T DeserializeObject<T>(XmlTextReader reader) where T : class
		{
			var target_type = typeof(T);
			string cur_target_prop_name = "";
			while(reader.Read()){
				switch(reader.LocalName){
				case "key":
					cur_target_prop_name = reader.ReadString();
					break;
					
				case "array":
					var child_reader = reader.ReadSubtree();
					child_reader.Read();
					var child_type = target_type.GetProperty(cur_target_prop_name, BindingFlags.Public).PropertyType;
					if(!child_type.IsArray)
						throw new Exception(string.Format("The property type of {0} isn't an array type!"), cur_target_prop_name);
					
					typeof(BVE5ResourceManager).GetMethod("DeserializeObject").MakeGenericMethod(child_type).Invoke(this, child_reader);
					break;
					
				case "dict":
					break;
					
				case "string":
					break;
					
				default:
					throw new Exception("A plist cannot have that type of data: " + reader.LocalName);
				}
			}
			return (T)null;
		}
		
		static object ReadObject(XmlTextReader reader)
		{
			return null;
		}*/
		
		static BVE5ResourceManager()
		{
			//TODO: read semantic information from xml files
			/*using(var s = typeof(BVE5ResourceManager).Assembly.GetManifestResourceStream("BVE5ResourceManager.BVE5BuiltinNames.xml")){
				using(var reader = new XmlTextReader(s)){
					
				}
			}*/
			var resource_path = Path.Combine(Path.GetDirectoryName(typeof(BVE5ResourceManager).Assembly.Location), @"resources\BVE5BuiltinNames.json");
			var builtin_names = JsonConvert.DeserializeObject<BuiltinsDefinition>(File.ReadAllText(resource_path));
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
