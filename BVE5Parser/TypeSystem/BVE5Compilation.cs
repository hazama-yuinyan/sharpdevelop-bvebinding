using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;

namespace BVE5Language.TypeSystem
{
    /// <summary>
    /// A BVE5Compilation represents a resolved BVE5 route project. It may include other resources such as images and sounds
    /// as well as text files.
    /// </summary>
    public class BVE5Compilation : ICompilation
    {
        private readonly ISolutionSnapshot solution_snapshot;
        private readonly ITypeResolveContext context;
        private readonly CacheManager cache_manager = new CacheManager();
        private readonly IAssembly main_assembly;
        private readonly IList<IAssembly> assemblies;
        private readonly IList<IAssembly> referenced_assemblies;
        private readonly PrimitiveTypeCache type_cache;

        #region ICompilation members
        public IList<IAssembly> Assemblies
        {
            get { return assemblies; }
        }

        public CacheManager CacheManager
        {
            get { return cache_manager; }
        }

        public IType FindType(KnownTypeCode typeCode)
        {
        	switch(typeCode){
        	case KnownTypeCode.Void:
        		return FindType(BVEPrimitiveTypeCode.None);
        			
        	case KnownTypeCode.Int32:
        		return FindType(BVEPrimitiveTypeCode.Integer);
        	
        	case KnownTypeCode.Double:
        		return FindType(BVEPrimitiveTypeCode.Float);
        		
        	case KnownTypeCode.String:
        		return FindType(BVEPrimitiveTypeCode.Name);
        		
        	default:
        		return new UnknownType("global", typeCode.ToString(), 0);
        	}
        }
        
        public IType FindType(BVEPrimitiveTypeCode typeCode)
        {
        	return type_cache.FindType(typeCode);
        }

        public INamespace GetNamespaceForExternAlias(string alias)
        {
            throw new NotImplementedException("Never implemented");
        }

        public IAssembly MainAssembly
        {
            get { return main_assembly; }
        }

        public StringComparer NameComparer
        {
            get { return StringComparer.Ordinal; }
        }

        public IList<IAssembly> ReferencedAssemblies
        {
            get { return referenced_assemblies; }
        }

        public INamespace RootNamespace
        {
            get { throw new NotImplementedException("Never implemented"); }
        }

        public ISolutionSnapshot SolutionSnapshot
        {
            get { return solution_snapshot; }
        }

        public ITypeResolveContext TypeResolveContext
        {
            get { return context; }
        }
        #endregion

        #region Constructor
        public BVE5Compilation(ISolutionSnapshot solutionSnapshot, IUnresolvedAssembly mainAssembly,
                               IEnumerable<IAssemblyReference> assemblyReferences)
        {
            if(solutionSnapshot == null)
                throw new ArgumentNullException("solutionSnapshot");

            if(mainAssembly == null)
                throw new ArgumentNullException("mainAssembly");

            if(assemblyReferences == null)
                throw new ArgumentNullException("assemblyReferences");

            solution_snapshot = solutionSnapshot;
            context = new SimpleTypeResolveContext(this);
            main_assembly = mainAssembly.Resolve(context);
            var assemblies = new List<IAssembly>{main_assembly};
            var referenced_assemblies = new List<IAssembly>();
            foreach(var asm_ref in assemblyReferences){
                IAssembly asm = asm_ref.Resolve(context);
                if(asm != null && !assemblies.Contains(asm))
                    assemblies.Add(asm);

                if(asm != null && !referenced_assemblies.Contains(asm))
                    referenced_assemblies.Add(asm);
            }

            this.assemblies = assemblies.AsReadOnly();
            this.referenced_assemblies = referenced_assemblies.AsReadOnly();
            this.type_cache = new PrimitiveTypeCache(this);
        }
        #endregion
    }
}
