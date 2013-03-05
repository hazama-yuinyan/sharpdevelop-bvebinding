using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Newtonsoft.Json;

using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace BVE5Language.TypeSystem
{
    class ArgumentAnnotation
    {
        public string Name{get; set;}
        public string ParamType{get; set;}
    }

    class MemberAnnotation
    {
        public ArgumentAnnotation[] Args { get; set; }
        public string Doc { get; set; }
    }

    class SemanticInfo
    {
        public Dictionary<string, Dictionary<string, MemberAnnotation[]>> SemanticInfos { get; set; }
    }

    /// <summary>
    /// Contains the unresolved assembly instance for BVE5's primitive and builtin types.
    /// </summary>
    [Serializable]
    public static class BVEBuiltins
    {
        internal const int PrimitiveTypeCodeCount = (int)BVEPrimitiveTypeCode.EnumForwardDirection + 1;
        
        static IUnresolvedAssembly builtin_assembly = null;
        
        #region Initialization
        static IUnresolvedTypeDefinition[] GetPrimitiveTypeDefs()
        {
        	var types = new DefaultUnresolvedTypeDefinition[]{
        		new DefaultUnresolvedTypeDefinition("global", "void"),
        		new DefaultUnresolvedTypeDefinition("global", "int"),
        		new DefaultUnresolvedTypeDefinition("global", "float"),
        		new DefaultUnresolvedTypeDefinition("global", "name"),
        		new DefaultUnresolvedTypeDefinition("global", "filepath"),
        		new DefaultUnresolvedTypeDefinition("global", "time"),
        		new DefaultUnresolvedTypeDefinition("global", "enumtilt"),
        		new DefaultUnresolvedTypeDefinition("global", "enumdirection"),
        		new DefaultUnresolvedTypeDefinition("global", "enumforward")
        	};
        	types[0].Kind = TypeKind.Class;
        	types[1].Kind = TypeKind.Class;
        	types[2].Kind = TypeKind.Class;
        	types[3].Kind = TypeKind.Class;
        	types[4].Kind = TypeKind.Class;
        	types[5].Kind = TypeKind.Class;
        	
        	types[6].Kind = TypeKind.Enum;
        	types[6].Members.Add(MakeEnumField(types[5], "AlwaysHorizontal", 0));
        	types[6].Members.Add(MakeEnumField(types[5], "RelatedToGradient", 1));
        	types[6].Members.Add(MakeEnumField(types[5], "RelatedToCant", 2));
        	types[6].Members.Add(MakeEnumField(types[5], "RelatedToGradientAndCant", 3));
        	
        	types[7].Kind = TypeKind.Enum;
        	types[7].Members.Add(MakeEnumField(types[6], "Left", -1));
        	types[7].Members.Add(MakeEnumField(types[6], "Right", 1));
        	
        	types[8].Kind = TypeKind.Enum;
        	types[8].Members.Add(MakeEnumField(types[7], "For", -1));
        	types[8].Members.Add(MakeEnumField(types[7], "Against", 1));
        	
        	return types;
        }
        
        static IUnresolvedMember MakeEnumField(IUnresolvedTypeDefinition type, string name, object value)
        {
        	var field = new DefaultUnresolvedField(type, name);
        	field.Accessibility = Accessibility.Public;
        	field.IsReadOnly = true;
        	field.IsStatic = true;
        	field.ReturnType = type;
        	field.ConstantValue = CreateSimpleConstantValue(type, value);
        	return field;
        }
        
        static IConstantValue CreateSimpleConstantValue(ITypeReference type, object value)
        {
        	return new SimpleConstantValue(type, value);
        }
        
        /// <summary>
        /// Gets an assembly that contains BVE5's primitive and builtin types.
        /// </summary>
        /// <returns></returns>
        public static IUnresolvedAssembly GetBuiltinAssembly()
        {
            if(builtin_assembly == null){
                var builtin_asm = new DefaultUnresolvedAssembly("BVE5Builtin");
                foreach(var primitive_type in GetPrimitiveTypeDefs())
                	builtin_asm.AddTypeDefinition(primitive_type);
                
                var resource_path = Path.Combine(Path.GetDirectoryName(typeof(BVE5ResourceManager).Assembly.Location), @"resources\BVE5SemanticInfos.json");
                var semantic_info = JsonConvert.DeserializeObject<SemanticInfo>(File.ReadAllText(resource_path));
                foreach(var type_name in semantic_info.SemanticInfos.Keys){
                    var cur_type_def = new DefaultUnresolvedTypeDefinition("global", type_name);
                    InitTypeDefinition(semantic_info.SemanticInfos[type_name], cur_type_def);
                    builtin_asm.AddTypeDefinition(cur_type_def);
                }

                builtin_assembly = builtin_asm;
            }

            return builtin_assembly;
        }

        static void InitTypeDefinition(Dictionary<string, MemberAnnotation[]> typeSemanticInfo, DefaultUnresolvedTypeDefinition typeDef)
        {
            foreach(var method_name in typeSemanticInfo.Keys){
                var method_info = typeSemanticInfo[method_name];
                EntityType type = EntityType.Method;

                foreach(var method_overload in method_info){
                    var m = new DefaultUnresolvedMethod(typeDef, method_name);
                    m.EntityType = type;
                    m.HasBody = false;
                    m.ReturnType = PrimitiveTypeReference.Void;     //All BVE5 methods have the return type 'void'

                    if(method_overload.Args.Length > 0){
                        foreach(var arg_annot in method_overload.Args)
                            m.Parameters.Add(TransformParameterInfo(arg_annot));
                    }

                    typeDef.Members.Add(m);
                }
            }
        }

        static IUnresolvedParameter TransformParameterInfo(ArgumentAnnotation argAnnotation)
        {
            var name = argAnnotation.Name;
            bool has_variable_params = false;
            if(name.EndsWith(".")){		//parameter names that end with trailing "..." indicate that the parameters can take arbitrary number of arguments
                has_variable_params = true;
                name = name.Substring(0, name.Length - 3);
            }
            var param = new DefaultUnresolvedParameter(PrimitiveTypeReference.Get(argAnnotation.ParamType), name);
            param.IsParams = has_variable_params;
            return param;
        }
        #endregion
    }
}
