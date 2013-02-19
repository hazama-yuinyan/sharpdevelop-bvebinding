using System;
using System.Collections.Generic;

using ICSharpCode.NRefactory.TypeSystem;

namespace BVE5Language.TypeSystem
{
	public sealed class BVE5NameResolveContext
	{
		private readonly IAssembly assembly;
		private readonly IList<IField> defined_names;
		
		public BVE5NameResolveContext(IAssembly assembly, IList<IField> names)
		{
			if (assembly == null)
				throw new ArgumentNullException("assembly");

			this.assembly = assembly;
			this.defined_names = names;
		}
		
		public ICompilation Compilation {
			get { return assembly.Compilation; }
		}
		
		public IAssembly CurrentAssembly {
			get { return assembly; }
		}
		
		public IList<IField> DefinedNames {
			get { return defined_names; }
		}
	}
}

