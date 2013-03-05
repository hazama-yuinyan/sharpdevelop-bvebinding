using System;
using System.Collections.Generic;
using System.Linq;

using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory;

namespace BVE5Language.Resolver
{
	/// <summary>
	/// Represents a group of methods.
	/// A method reference used to create a delegate is resolved to a MethodGroupResolveResult.
	/// The MethodGroupResolveResult has no type.
	/// To retrieve the delegate type or the chosen overload, look at the method group conversion.
	/// </summary>
	public class MethodGroupResolveResult : ResolveResult
	{
		readonly IList<IMethod> method_lists;
		readonly ResolveResult target_result;
		readonly string method_name;
		
		public MethodGroupResolveResult(ResolveResult targetResult, string methodName, IList<IMethod> methods)
            : base(SpecialType.UnknownType)
		{
			if(methods == null)
				throw new ArgumentNullException("methods");

			target_result = targetResult;
			method_name = methodName;
			method_lists = methods;
		}
		
		/// <summary>
		/// Gets the resolve result for the target object.
		/// </summary>
		public ResolveResult TargetResult {
			get { return target_result; }
		}
		
		/// <summary>
		/// Gets the type of the reference to the target object.
		/// </summary>
		public IType TargetType {
			get { return target_result != null ? target_result.Type : SpecialType.UnknownType; }
		}
		
		/// <summary>
		/// Gets the name of the methods in this group.
		/// </summary>
		public string MethodName {
			get { return method_name; }
		}
		
		/// <summary>
		/// Gets the methods that were found.
		/// This list does not include extension methods.
		/// </summary>
		public IEnumerable<IMethod> Methods {
			get { return method_lists; }
		}
		
		public override string ToString()
		{
			return string.Format("[{0} with {1} method(s)]", GetType().Name, this.Methods.Count());
		}

        public OverloadResolution PerformOverloadResolution(ICompilation compilation, ResolveResult[] arguments)
		{
			Log.WriteLine("Performing overload resolution for " + this);
			Log.WriteCollection("  Arguments: ", arguments);
			
			var or = new OverloadResolution(compilation, arguments);
			
			or.AddMethodList(method_lists);
			
			Log.WriteLine("Overload resolution finished, best candidate is {0}.", or.BestCandidate);
			return or;
		}
		
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			if(target_result != null)
				return new[] { target_result };
			else
				return Enumerable.Empty<ResolveResult>();
		}
	}
}

