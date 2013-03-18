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
using System.Text.RegularExpressions;
using System.Xml;
using BVE5Language.TypeSystem;
using Newtonsoft.Json;

namespace BVE5Language
{
	class BuiltinsDefinition
	{
		public string[] TypeNames{get; set;}
		public Dictionary<string, string[]> Methods{get; set;}
	}
		
	public class CommonFileCommandInfo
	{
		public ArgumentAnnotation[] Args{get; set;}
		public string Doc{get; set;}
	}
		
	public class InitFileArgumentAnnotation
	{
		public string Name{get; set;}
		public string ParamType{get; set;}
		public string Doc{get; set;}
	}
		
	public class InitFileMemberAnnotation
	{
		public InitFileArgumentAnnotation[] Keys { get; set; }
       	public string Doc { get; set; }
	}
		
	public class InitFileSemanticInfo
    {
      	public Dictionary<string, InitFileMemberAnnotation> SemanticInfos { get; set; }
    }
		
	public class ArgumentAnnotation
	{
	    public string Name{get; set;}
	    public string ParamType{get; set;}
	}
	
	public class MemberAnnotation
	{
		public ArgumentAnnotation[] Args { get; set; }
	    public string Doc { get; set; }
	}
	
	class SemanticInfo
	{
		public Dictionary<string, Dictionary<string, MemberAnnotation[]>> SemanticInfos { get; set; }
	}
	    
	/// <summary>
	/// Description of BVE5ResourceManager.
	/// </summary>
	public static class BVE5ResourceManager
	{
		static readonly string[] TypeNames;
		static readonly Dictionary<string, string[]> MethodNames;
		public static readonly Dictionary<string, Dictionary<string, MemberAnnotation[]>> RouteFileSemanticInfos;
		public static readonly Dictionary<string, CommonFileCommandInfo> CommonFileSemanticInfos;
		public static readonly Dictionary<string, InitFileSemanticInfo> InitFileSemanticInfos;
		static readonly Dictionary<string, string> Documentations;
		static readonly Regex VariableNameFinder = new Regex(@"\$(.+)", RegexOptions.Compiled);
		
		static BVE5ResourceManager()
		{
			var builtin_names = JsonConvert.DeserializeObject<BuiltinsDefinition>(GetResourceString("BVE5LanguageResources.BuiltinNames.json"));
			TypeNames = builtin_names.TypeNames;
			MethodNames = builtin_names.Methods;
			
			CommonFileSemanticInfos = new Dictionary<string, CommonFileCommandInfo>{
				{"SignalAspectsList", JsonConvert.DeserializeObject<CommonFileCommandInfo>(GetResourceString("BVE5LanguageResources.SignalAspectsListSemanticInfos.json"))},
				{"SoundList", JsonConvert.DeserializeObject<CommonFileCommandInfo>(GetResourceString("BVE5LanguageResources.SoundListSemanticInfos.json"))},
				{"StationList", JsonConvert.DeserializeObject<CommonFileCommandInfo>(GetResourceString("BVE5LanguageResources.StationListSemanticInfos.json"))},
				{"StructureList", JsonConvert.DeserializeObject<CommonFileCommandInfo>(GetResourceString("BVE5LanguageResources.StructureListSemanticInfos.json"))}
			};
			
			InitFileSemanticInfos = new Dictionary<string, InitFileSemanticInfo>{
				{"TrainFile", JsonConvert.DeserializeObject<InitFileSemanticInfo>(GetResourceString("BVE5LanguageResources.TrainFileSemanticInfos.json"))},
				{"VehicleParametersFile", JsonConvert.DeserializeObject<InitFileSemanticInfo>(GetResourceString("BVE5LanguageResources.VehicleParametersFileSemanticInfos.json"))},
				{"InstrumentPanelFile", JsonConvert.DeserializeObject<InitFileSemanticInfo>(GetResourceString("BVE5LanguageResources.InstrumentPanelFileSemanticInfos.json"))},
				{"VehicleSoundFile", JsonConvert.DeserializeObject<InitFileSemanticInfo>(GetResourceString("BVE5LanguageResources.VehicleSoundFileSemanticInfos.json"))}
			};
			
			Documentations = JsonConvert.DeserializeObject<Dictionary<string, string>>(GetResourceString("BVE5LanguageResources.Documentation.json"));
			
			RouteFileSemanticInfos = JsonConvert.DeserializeObject<SemanticInfo>(GetResourceString("BVE5LanguageResources.SemanticInfos.json")).SemanticInfos;
		}
		
		internal static string GetResourceString(string resourceName)
		{
			var resource_stream = typeof(BVE5ResourceManager).Assembly.GetManifestResourceStream(resourceName);
			return new StreamReader(resource_stream).ReadToEnd();
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
        
        public static MemberAnnotation[] GetRouteFileMemberAnnotation(string typeName, string methodName)
        {
        	var type_semantic_info = RouteFileSemanticInfos[typeName];
        	return type_semantic_info[methodName];
        }
        
        public static string GetDocumentationString(string docName)
        {
        	var match = VariableNameFinder.Match(docName);
        	if(!match.Success)
        		throw new InvalidOperationException("Unknown type of input! Variables must begin with a '$'!");
        	
        	return Documentations[match.Groups[1].Value];
        }
	}
}
