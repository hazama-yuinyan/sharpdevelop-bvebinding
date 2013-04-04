using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Utils;

namespace BVE5Language.TypeSystem
{
    public class BVE5ProjectContent : IProjectContent
    {
        string assembly_name;
        string full_assembly_name;
        string project_file_name;
        string location;
        Dictionary<string, IUnresolvedFile> unresolved_files;
        List<IAssemblyReference> assembly_references;

        #region Constructors
        public BVE5ProjectContent()
		{
			unresolved_files = new Dictionary<string, IUnresolvedFile>(Platform.FileNameComparer);
			assembly_references = new List<IAssemblyReference>();
		}
		
		protected BVE5ProjectContent(BVE5ProjectContent pc)
		{
			assembly_name = pc.assembly_name;
			full_assembly_name = pc.full_assembly_name;
			project_file_name = pc.project_file_name;
			location = pc.location;
			unresolved_files = new Dictionary<string, IUnresolvedFile>(pc.unresolved_files, Platform.FileNameComparer);
			assembly_references = new List<IAssemblyReference>(pc.assembly_references);
		}
        #endregion

        protected virtual BVE5ProjectContent Clone()
        {
            return new BVE5ProjectContent(this);
        }

        #region IProjectContent members
        public IProjectContent AddAssemblyReferences(params IAssemblyReference[] references)
        {
            var cloned = Clone();
            cloned.assembly_references.AddRange(references);
            return cloned;
        }

        public IProjectContent AddAssemblyReferences(IEnumerable<IAssemblyReference> references)
        {
            return AddAssemblyReferences(references.ToArray());
        }

        /// <summary>
        /// Adds the specified files to the project content.
        /// If a file with the same name already exists, update the existing file.
        /// </summary>
        public IProjectContent AddOrUpdateFiles(params IUnresolvedFile[] newFiles)
        {
            var cloned = Clone();
            foreach(var file in newFiles)
                cloned.unresolved_files[file.FileName] = file;

            return cloned;
        }

        /// <summary>
        /// Adds the specified files to the project content.
        /// If a file with the same name already exists, this method updates the existing file.
        /// </summary>
        public IProjectContent AddOrUpdateFiles(IEnumerable<IUnresolvedFile> newFiles)
        {
            return AddOrUpdateFiles(newFiles.ToArray());
        }

        public IEnumerable<IAssemblyReference> AssemblyReferences
        {
            get { return assembly_references; }
        }

        public object CompilerSettings{
            get { throw new NotImplementedException(); }
        }

        public ICompilation CreateCompilation(ISolutionSnapshot solutionSnapshot)
        {
            return new BVE5Compilation(solutionSnapshot, this, assembly_references);
        }

        public ICompilation CreateCompilation()
        {
            var snapshot = new DefaultSolutionSnapshot();
            var compilation = new BVE5Compilation(snapshot, this, assembly_references);
            snapshot.AddCompilation(this, compilation);
            return compilation;
        }

        public IEnumerable<IUnresolvedFile> Files{
            get { return unresolved_files.Values; }
        }

        public IUnresolvedFile GetFile(string fileName)
        {
            IUnresolvedFile file;
            if(unresolved_files.TryGetValue(fileName, out file))
                return file;
            else
                return null;
        }

        public string ProjectFileName{
            get { return project_file_name; }
        }

        public IProjectContent RemoveAssemblyReferences(params IAssemblyReference[] references)
        {
            var cloned = Clone();
            foreach(var reference in references)
                cloned.assembly_references.Remove(reference);

            return cloned;
        }

        public IProjectContent RemoveAssemblyReferences(IEnumerable<IAssemblyReference> references)
        {
            return RemoveAssemblyReferences(references.ToArray());
        }

        /// <summary>
        /// Removes the files with the specified names.
        /// </summary>
        public IProjectContent RemoveFiles(params string[] fileNames)
        {
            return RemoveFiles((IEnumerable<string>)fileNames);
        }

        /// <summary>
        /// Removes the files with the specified names.
        /// </summary>
        public IProjectContent RemoveFiles(IEnumerable<string> fileNames)
        {
            BVE5ProjectContent pc = Clone();
            foreach(var file_name in fileNames)
                pc.unresolved_files.Remove(file_name);

            return pc;
        }

        /// <summary>
        /// Sets both the short and the full assembly names.
        /// </summary>
        /// <param name="newAssemblyName">New full assembly name.</param>
        public IProjectContent SetAssemblyName(string newAssemblyName)
        {
            var cloned = Clone();
            cloned.full_assembly_name = newAssemblyName;
            cloned.assembly_name = newAssemblyName;
            return cloned;
        }

        public IProjectContent SetCompilerSettings(object compilerSettings)
        {
            throw new NotImplementedException();
        }

        public IProjectContent SetLocation(string newLocation)
        {
            var cloned = Clone();
            cloned.location = newLocation;
            return cloned;
        }

        public IProjectContent SetProjectFileName(string newProjectFileName)
        {
            var cloned = Clone();
            cloned.project_file_name = newProjectFileName;
            return cloned;
        }

        [Obsolete("Use RemoveFiles/AddOrUpdateFiles instead")]
        public IProjectContent UpdateProjectContent(IEnumerable<IUnresolvedFile> oldFiles, IEnumerable<IUnresolvedFile> newFiles)
        {
            throw new NotImplementedException();
        }

        [Obsolete("Use RemoveFiles/AddOrUpdateFiles instead")]
        public IProjectContent UpdateProjectContent(IUnresolvedFile oldFile, IUnresolvedFile newFile)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region IUnresolvedAssembly members
        public IEnumerable<IUnresolvedAttribute> AssemblyAttributes{
            get{return Files.SelectMany(f => f.AssemblyAttributes);}
        }

        public string AssemblyName{
            get { return assembly_name; }
        }

        public string FullAssemblyName{
            get { return full_assembly_name; }
        }

        public string Location{
            get { return location; }
        }

        public IEnumerable<IUnresolvedAttribute> ModuleAttributes{
            get { return Files.SelectMany(f => f.ModuleAttributes); }
        }

        public IEnumerable<IUnresolvedTypeDefinition> TopLevelTypeDefinitions{
            get{
        		return Files.SelectMany(f => f.TopLevelTypeDefinitions)
        			.Concat(
        				AssemblyReferences.Cast<IUnresolvedAssembly>()
        			        .SelectMany(r => r.TopLevelTypeDefinitions)
        			);
        	}
        }
        #endregion

        #region IAssemblyReference members
        public IAssembly Resolve(ITypeResolveContext context)
        {
            if(context == null)
                throw new ArgumentNullException("context");
            
            var cache = context.Compilation.CacheManager;
            IAssembly asm = (IAssembly)cache.GetShared(this);
            if(asm != null){
                return asm;
            }else{
                asm = new BVE5Assembly(context.Compilation, this);
                return (IAssembly)cache.GetOrAddShared(this, asm);
            }
        }
        #endregion
    }
}
