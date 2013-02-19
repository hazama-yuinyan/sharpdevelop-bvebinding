using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ICSharpCode.NRefactory.TypeSystem;

namespace BVE5Language.TypeSystem
{
    public class PrimitiveTypeReference : ITypeReference
    {
        #region Public static fields
        public static PrimitiveTypeReference Void = new PrimitiveTypeReference(BVEPrimitiveTypeCode.None, "void");

        public static PrimitiveTypeReference Integer = new PrimitiveTypeReference(BVEPrimitiveTypeCode.Integer, "int");

        public static PrimitiveTypeReference Float = new PrimitiveTypeReference(BVEPrimitiveTypeCode.Float, "float");

        public static PrimitiveTypeReference NameType = new PrimitiveTypeReference(BVEPrimitiveTypeCode.Name, "name");

        public static PrimitiveTypeReference Path = new PrimitiveTypeReference(BVEPrimitiveTypeCode.FilePath, "path");

        public static PrimitiveTypeReference Time = new PrimitiveTypeReference(BVEPrimitiveTypeCode.Time, "time");

        public static PrimitiveTypeReference EnumTilt = new PrimitiveTypeReference(BVEPrimitiveTypeCode.EnumTilt, "enum<Tilt>");

        public static PrimitiveTypeReference EnumDirection = new PrimitiveTypeReference(BVEPrimitiveTypeCode.EnumDirection, "enum<Direction>");

        public static PrimitiveTypeReference EnumForward =
            new PrimitiveTypeReference(BVEPrimitiveTypeCode.EnumForwardDirection, "enum<ForwardDirection>");
        #endregion

        private readonly BVEPrimitiveTypeCode type_code;
        private readonly string name;
        private readonly string @namespace;
        
        #region Properties
        public string Name{
        	get{return name;}
        }
        
        public string Namespace{
        	get{return @namespace;}
        }
        #endregion

        public PrimitiveTypeReference(BVEPrimitiveTypeCode typeCode, string typeName)
        {
            type_code = typeCode;
            name = typeName;
            @namespace = "global";
        }

        public static PrimitiveTypeReference Get(string typeName)
        {
            switch(typeName.ToLower()){
            case "none":
                return Void;

            case "int":
                return Integer;

            case "float":
                return Float;

            case "name":
                return NameType;

            case "filepath":
                return Path;

            case "timeformat":
                return Time;

            case "enum<tilt>":
                return EnumTilt;

            case "enum<direction>":
                return EnumDirection;

            case "enum<forwardingdirection>":
                return EnumForward;

            default:
                throw new InvalidOperationException(typeName + " is not a primitive data type name in BVE5!");
            }
        }
        
        public static PrimitiveTypeReference Get(BVEPrimitiveTypeCode typeCode)
        {
        	switch(typeCode){
        	case BVEPrimitiveTypeCode.None:
        		return Void;
        		
        	case BVEPrimitiveTypeCode.Integer:
        		return Integer;
        		
        	case BVEPrimitiveTypeCode.Float:
        		return Float;
        		
        	case BVEPrimitiveTypeCode.Name:
        		return NameType;
        		
        	case BVEPrimitiveTypeCode.FilePath:
        		return Path;
        		
        	case BVEPrimitiveTypeCode.Time:
        		return Time;
        		
        	case BVEPrimitiveTypeCode.EnumTilt:
        		return EnumTilt;
        		
        	case BVEPrimitiveTypeCode.EnumDirection:
        		return EnumDirection;
        		
        	case BVEPrimitiveTypeCode.EnumForwardDirection:
        		return EnumForward;
        		
        	default:
        		throw new InvalidOperationException();
        	}
        }

        #region ITypeReference member
        public IType Resolve(ITypeResolveContext context)
        {
        	var bve_compilation = context.Compilation as BVE5Compilation;
        	return (bve_compilation != null) ? bve_compilation.FindType(type_code) : null;
        }
        #endregion
    }
}
