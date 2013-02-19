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
    /// A BVE5Assembly represents a resolved source file content.
    /// </summary>
    public class BVE5Assembly : IAssembly
    {
        private readonly ICompilation compilation;
        private readonly ITypeResolveContext context;
        private readonly BVE5ProjectContent project_content;

        internal BVE5Assembly(ICompilation compilation, BVE5ProjectContent projectContent)
        {
            this.compilation = compilation;
            project_content = projectContent;
            context = new SimpleTypeResolveContext(this);
        }

        #region IAssembly members
        public IList<IAttribute> AssemblyAttributes
        {
            get { return null; }
        }

        public string AssemblyName
        {
            get { return project_content.AssemblyName; }
        }

        public string FullAssemblyName
        {
            get { return project_content.FullAssemblyName; }
        }

        Dictionary<TopLevelTypeName, ITypeDefinition> type_dict;

        private Dictionary<TopLevelTypeName, ITypeDefinition> GetTypes()
        {
            var dict = LazyInit.VolatileRead(ref this.type_dict);
            if(dict != null){
                return dict;
            }else{
                // Always use the ordinal comparer for the main dictionary so that partial classes
                // get merged correctly.
                // The compilation's comparer will be used for the per-namespace dictionaries.
                var comparer = TopLevelTypeNameComparer.Ordinal;
                dict = project_content.TopLevelTypeDefinitions
                    .GroupBy(t => new TopLevelTypeName(t.Namespace, t.Name, t.TypeParameters.Count), comparer)
                    .ToDictionary(g => g.Key, g => CreateResolvedTypeDefinition(g.ToArray()), comparer);
                return LazyInit.GetOrSet(ref this.type_dict, dict);
            }
        }

        ITypeDefinition CreateResolvedTypeDefinition(IUnresolvedTypeDefinition[] parts)
        {
            return new DefaultResolvedTypeDefinition(context, parts);
        }

        public ITypeDefinition GetTypeDefinition(TopLevelTypeName topLevelTypeName)
        {
            ITypeDefinition def;
            if(GetTypes().TryGetValue(topLevelTypeName, out def))
                return def;
            else
                return null;
        }

        public bool InternalsVisibleTo(IAssembly assembly)
        {
            throw new NotImplementedException();
        }

        public bool IsMainAssembly{
            get { return compilation.MainAssembly == this; }
        }

        public IList<IAttribute> ModuleAttributes{
            get { return null; }
        }

        public INamespace RootNamespace{
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<ITypeDefinition> TopLevelTypeDefinitions{
            get { throw new NotImplementedException(); }
        }

        public IUnresolvedAssembly UnresolvedAssembly{
            get { return project_content; }
        }
        #endregion

        #region ICompilationProvider member
        public ICompilation Compilation{
            get { return compilation; }
        }
        #endregion

        public override string ToString()
        {
            return "[BVE5Assembly " + AssemblyName + "]";
        }
    }
}
