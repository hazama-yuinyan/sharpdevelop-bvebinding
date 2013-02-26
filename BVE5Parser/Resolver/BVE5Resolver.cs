using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Newtonsoft.Json;

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

using BVE5Language.TypeSystem;

namespace BVE5Language.Resolver
{
	/// <summary>
	/// Contains the main resolver logic.
	/// </summary>
	/// <remarks>
	/// This class is thread-safe.
	/// </remarks>
	public class BVE5Resolver
	{
		private static readonly ResolveResult ErrorResult = ErrorResolveResult.UnknownError;
		private readonly BVE5Compilation compilation;
		
		public BVE5Compilation Compilation{
			get{return compilation;}
		}

		public ITypeResolveContext CurrentTypeResolveContext{
			get{return compilation.TypeResolveContext;}
		}
		
		#region Constructor
		public BVE5Resolver(BVE5Compilation compilation)
		{
			if(compilation == null)
				throw new ArgumentNullException("compilation");
			
			this.compilation = compilation;
		}
		#endregion

        private IType GetBuitlinTypeDefinition(string typeName)
        {
        	var type_name = new TopLevelTypeName("global", typeName);
        	foreach(var asm in compilation.Assemblies){
        		var type_def = asm.GetTypeDefinition(type_name);
        		if(type_def != null)
        			return type_def;
        	}
        	
        	return SpecialType.UnknownType;
        }

        public ResolveResult LookupMethodName(TypeResolveResult typeResolveResult, string typeName, string name)
        {
            if(!BVE5ResourceManager.BuiltinTypeHasMethod(typeName, name))
            	return new UnknownMemberResolveResult(GetBuitlinTypeDefinition(typeName), name, EmptyList<IType>.Instance);

            var type_def = typeResolveResult.Type as ITypeDefinition;
            if(type_def == null)
            	throw new Exception("Internal: target type is not a type definition!");
            
            return new MethodGroupResolveResult(typeResolveResult, name, new List<IMethod>(type_def.GetMethods((member) => member.Name == name)));
        }

        #region Resolve Member access
        public ResolveResult ResolveMemberAccess(ResolveResult targetResult, string name)
        {
            if(targetResult == null)
                throw new ArgumentNullException("targetResult");

            var type_rr = targetResult as TypeResolveResult;
            if(type_rr != null)
                return LookupMethodName(type_rr, type_rr.Type.Name, name);
            else
                return new ErrorResolveResult(targetResult.Type);
        }
        #endregion

        public ResolveResult ResolveIndexer(ResolveResult targetResult, string indexName)
        {
            if(targetResult == null)
                throw new ArgumentNullException("targetResult");

            var type_rr = targetResult as TypeResolveResult;
            if(type_rr != null)	//TODO: implement it
                return null;

            return new ErrorResolveResult(targetResult.Type);
        }

        #region Resolve invocation
        public ResolveResult ResolveInvocation(ResolveResult targetResult, ResolveResult[] arguments)
        {
            MethodGroupResolveResult mgrr = targetResult as MethodGroupResolveResult;
            if(mgrr != null){
                var or = mgrr.PerformOverloadResolution(compilation, arguments);
                if(or.BestCandidate != null)
                    return or.CreateResolveResult(mgrr);
            }

            UnknownMemberResolveResult umrr = targetResult as UnknownMemberResolveResult;
            if(umrr != null)
                return new UnknownMethodResolveResult(umrr.TargetType, umrr.MemberName, EmptyList<IType>.Instance, CreateParameters(arguments));
            else
                return ErrorResult;
        }

        private List<IParameter> CreateParameters(ResolveResult[] args)
        {
            var result = new List<IParameter>();
            string arg_name;
            for(int i = 0; i < args.Length; ++i){
                arg_name = GuessParameterName(args[i]);
                result.Add(new DefaultParameter(args[i].Type, arg_name));
            }
            return result;
        }

        private static string GuessParameterName(ResolveResult rr)
        {
            MemberResolveResult mrr = rr as MemberResolveResult;
            if(mrr != null)
                return mrr.Member.Name;

            UnknownMemberResolveResult umrr = rr as UnknownMemberResolveResult;
            if(umrr != null)
                return umrr.MemberName;

            if(rr.Type.Kind != TypeKind.Unknown && !string.IsNullOrEmpty(rr.Type.Name))
                return MakeParameterName(rr.Type.Name);
            else
                return "parameter";
        }

        private static string MakeParameterName(string variableName)
        {
            if(string.IsNullOrEmpty(variableName))
                return "parameter";

            if(variableName.Length > 1 && variableName[0] == '_')
                variableName = variableName.Substring(1);
            
            return char.ToLower(variableName[0]) + variableName.Substring(1);
        }
        #endregion

        #region Resolve Primitive
        public ResolveResult ResolvePrimitive(object value)
        {
            TypeCode typeCode = Type.GetTypeCode(value.GetType());
            IType type = compilation.FindType(typeCode);
            return new ConstantResolveResult(type, value);
        }
        #endregion
        
        public ResolveResult ResolveTypeName(string typeName)
        {
        	if(!BVE5ResourceManager.IsBuiltinTypeName(typeName) && !BVE5ResourceManager.IsBuiltinTypeName(typeName))	//First try to match in case-sensitive way and then in case-insensitive way
        		return ErrorResult;
        	
        	var type_name = new TopLevelTypeName("global", typeName, 0);
        	foreach(var asm in compilation.ReferencedAssemblies){
        		var type_def = asm.GetTypeDefinition(type_name);
        		if(type_def != null)
        			return new TypeResolveResult(type_def);
        	}
        	return ErrorResult;
        }

        public ResolveResult ResolvePositionStatement(int position, IType type)
        {
            return new Semantics.PositionStatementResolveResult(position, type);
        }
        
        public ResolveResult ResolveTimeLiteral(Ast.TimeFormatLiteral timeLiteral)
		{
            if(timeLiteral.Hour < 0 || timeLiteral.Hour>= 24)
            	return new ErrorResolveResult(compilation.FindType(BVEPrimitiveTypeCode.Time),
            	                              "'Hour' must be between 0(inclusive) and 24(exclusive)", timeLiteral.StartLocation);

            if(timeLiteral.Minute < 0 || timeLiteral.Minute >= 60)
            	return new ErrorResolveResult(compilation.FindType(BVEPrimitiveTypeCode.Time),
            	                              "'Minute' must be between 0(inclusive) and 60(exclusive)", timeLiteral.StartLocation);

            if(timeLiteral.Second < 0 || timeLiteral.Second >= 60)
            	return new ErrorResolveResult(compilation.FindType(BVEPrimitiveTypeCode.Time),
            	                              "'Second' must be between 0(inclusive) and 60(exclusive)", timeLiteral.StartLocation);

            return new TypeResolveResult(compilation.FindType(BVEPrimitiveTypeCode.Time));
		}
    }
}

