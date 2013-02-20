using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace BVE5Language.Resolver
{
	/// <summary>
	/// BVE5 overload resolution.
	/// </summary>
	public class OverloadResolution
	{
		sealed class Candidate
		{
			public readonly IParameterizedMember Member;
			
			/// <summary>
			/// Gets the parameter types. In the first step, these are the types without any substition.
			/// After type inference, substitutions will be performed.
			/// </summary>
			public readonly IType[] ParameterTypes;

            /// <summary>
            /// Returns the normal form candidate, if this is an expanded candidate.
            /// </summary>
            public readonly bool IsExpandedForm;
			
			/// <summary>
			/// argument index -> parameter index; -1 for arguments that could not be mapped
			/// </summary>
			public int[] ArgumentToParameterMap;
			
			public OverloadResolutionErrors Errors;
			public int ErrorCount;
			
			/// <summary>
			/// Gets the original member parameters
			/// </summary>
			public readonly IList<IParameter> Parameters;
			
			public Candidate(IParameterizedMember member, bool isExpanded)
			{
				this.Member = member;
                this.IsExpandedForm = isExpanded;
				this.Parameters = member.Parameters;
				this.ParameterTypes = new IType[this.Parameters.Count];
			}
			
			public void AddError(OverloadResolutionErrors newError)
			{
				this.Errors |= newError;
				if(!IsApplicable(newError))
					this.ErrorCount++;
			}
		}
		
		private readonly ICompilation compilation;
		private readonly ResolveResult[] arguments;
		List<Candidate> candidates = new List<Candidate>();
		Candidate best_candidate;
		Candidate best_candidate_ambiguous_with;
		bool best_candidate_was_validated;
		OverloadResolutionErrors best_candidate_validation_result;
		
		#region Constructor
		public OverloadResolution(ICompilation compilation, ResolveResult[] arguments)
		{
			if(compilation == null)
				throw new ArgumentNullException("compilation");

			if(arguments == null)
				throw new ArgumentNullException("arguments");

			this.compilation = compilation;
			this.arguments = arguments;
		}
		#endregion
		
		#region Input Properties
		/// <summary>
		/// Gets the arguments for which this OverloadResolution instance was created.
		/// </summary>
		public IList<ResolveResult> Arguments {
			get { return arguments; }
		}
		#endregion
		
		#region AddCandidate
		/// <summary>
		/// Adds a candidate to overload resolution.
		/// </summary>
		/// <param name="member">The candidate member to add.</param>
		/// <returns>The errors that prevent the member from being applicable, if any.
		/// Note: this method does not return errors that do not affect applicability.</returns>
		public OverloadResolutionErrors AddCandidate(IParameterizedMember member)
		{
			return AddCandidate(member, OverloadResolutionErrors.None);
		}
		
		/// <summary>
		/// Adds a candidate to overload resolution.
		/// </summary>
		/// <param name="member">The candidate member to add.</param>
		/// <param name="additionalErrors">Additional errors that apply to the candidate.
		/// This is used to represent errors during member lookup (e.g. OverloadResolutionErrors.Inaccessible)
		/// in overload resolution.</param>
		/// <returns>The errors that prevent the member from being applicable, if any.
		/// Note: this method does not return errors that do not affect applicability.</returns>
		public OverloadResolutionErrors AddCandidate(IParameterizedMember member, OverloadResolutionErrors additionalErrors)
		{
			if(member == null)
				throw new ArgumentNullException("member");
			
			Candidate c = new Candidate(member, false);
			c.AddError(additionalErrors);
			if(CalculateCandidate(c)){
				//candidates.Add(c);
			}

            if(member.Parameters.Count > 0 && member.Parameters[member.Parameters.Count - 1].IsParams){
                var expanded_candidate = new Candidate(member, true);
                expanded_candidate.AddError(additionalErrors);
                // consider expanded form only if it isn't obviously wrong
                if(CalculateCandidate(expanded_candidate)){
                    //candidates.Add(expandedCandidate);

                    if(expanded_candidate.ErrorCount < c.ErrorCount)
                        return expanded_candidate.Errors;
                }
            }
			
			return c.Errors;
		}
		
		/// <summary>
		/// Calculates applicability etc. for the candidate.
		/// </summary>
		/// <returns>True if the calculation was successful, false if the candidate should be removed without reporting an error</returns>
		bool CalculateCandidate(Candidate candidate)
		{
			if(!ResolveParameterTypes(candidate))
				return false;

			MapCorrespondingParameters(candidate);
			CheckApplicability(candidate);
			ConsiderIfNewCandidateIsBest(candidate);
			return true;
		}
		
		bool ResolveParameterTypes(Candidate candidate)
		{
			for(int i = 0; i < candidate.Parameters.Count; ++i){
				IType type = candidate.Parameters[i].Type;
				if(candidate.IsExpandedForm && i == candidate.Parameters.Count - 1){
					ArrayType arrayType = type as ArrayType;
					if(arrayType != null && arrayType.Dimensions == 1)
						type = arrayType.ElementType;
					else
						return false; // error: cannot unpack params-array. abort considering the expanded form for this candidate
				}
				candidate.ParameterTypes[i] = type;
			}
			return true;
		}
		#endregion
		
		#region AddMethodList
		/// <summary>
		/// Adds all candidates from the method lists.
		/// 
		/// This method implements the logic that causes applicable methods in derived types to hide
		/// all methods in base types.
		/// </summary>
		/// <param name="methodList">The methods, grouped by declaring type. Base types must come first in the list.</param>
		public void AddMethodList(IList<IMethod> methodList)
		{
			if(methodList == null)
				throw new ArgumentNullException("methodLists");
			
            foreach(var method in methodList){
                Log.Indent();
                OverloadResolutionErrors errors = AddCandidate(method);
                Log.Unindent();
                LogCandidateAddingResult("  Candidate", method, errors);
			}
		}
		
		[Conditional("DEBUG")]
		internal void LogCandidateAddingResult(string text, IParameterizedMember method, OverloadResolutionErrors errors)
		{
#if DEBUG
			Log.WriteLine(string.Format("{0} {1} = {2}{3}",
			                            text, method,
			                            errors == OverloadResolutionErrors.None ? "Success" : errors.ToString(),
			                            this.BestCandidate == method ? " (best candidate so far)" :
			                            this.BestCandidateAmbiguousWith == method ? " (ambiguous)" : ""
			                            ));
#endif
		}
        #endregion
		
		#region MapCorrespondingParameters
		void MapCorrespondingParameters(Candidate candidate)
		{
			candidate.ArgumentToParameterMap = new int[arguments.Length];
			for(int i = 0; i < arguments.Length; ++i){
				candidate.ArgumentToParameterMap[i] = -1;
                // positional argument
                if(i < candidate.ParameterTypes.Length)
                    candidate.ArgumentToParameterMap[i] = i;
                else if(candidate.IsExpandedForm)
                    candidate.ArgumentToParameterMap[i] = candidate.ParameterTypes.Length - 1;
                else
                    candidate.AddError(OverloadResolutionErrors.TooManyPositionalArguments);
			}
		}
		#endregion
		
		#region CheckApplicability
		/// <summary>
		/// Returns whether a candidate with the given errors is still considered to be applicable.
		/// </summary>
		public static bool IsApplicable(OverloadResolutionErrors errors)
		{
			const OverloadResolutionErrors errorsThatDoNotMatterForApplicability = OverloadResolutionErrors.AmbiguousMatch;
			return (errors & ~errorsThatDoNotMatterForApplicability) == OverloadResolutionErrors.None;
		}

        private static bool CanConvertToEnum(ResolveResult rr, IType type)
        {
            Debug.Assert(rr.IsCompileTimeConstant);
            if(type.Kind == TypeKind.Enum)
                return type.GetFields().Any((field) => rr.ConstantValue == field.ConstantValue);
            else
                return false;
        }

        private static bool MatchType(ResolveResult resolveResult, IType paramType)
        {
            if(resolveResult.IsCompileTimeConstant)
                return CanConvertToEnum(resolveResult, paramType);
            else
                return resolveResult.Type == paramType;
        }
		
		void CheckApplicability(Candidate candidate)
		{
			// C# 4.0 spec: §7.5.3.1 Applicable function member
			
			// Test whether parameters were mapped the correct number of arguments:
			int[] argument_count_per_parameter = new int[candidate.ParameterTypes.Length];
			foreach(int parameter_index in candidate.ArgumentToParameterMap){
				if(parameter_index >= 0)
					argument_count_per_parameter[parameter_index]++;
			}

			for(int i = 0; i < argument_count_per_parameter.Length; ++i){
				if(candidate.IsExpandedForm && i == argument_count_per_parameter.Length - 1)
					continue; // any number of arguments is fine for the params-array
				
                if(argument_count_per_parameter[i] == 0)
					candidate.AddError(OverloadResolutionErrors.MissingArgumentForRequiredParameter);
				else if(argument_count_per_parameter[i] > 1)
					candidate.AddError(OverloadResolutionErrors.MultipleArgumentsForSingleParameter);
			}

            // Test whether types of arguments match that of parameters
            for(int i = 0; i < arguments.Length; ++i){
                int parameterIndex = candidate.ArgumentToParameterMap[i];
                IType parameter_type = candidate.ParameterTypes[parameterIndex];
                if(!MatchType(arguments[i], parameter_type))
                    candidate.AddError(OverloadResolutionErrors.ArgumentTypeMismatch);
            }
		}
        #endregion
		
		/*#region BetterFunctionMember
		/// <summary>
		/// Returns 1 if c1 is better than c2; 2 if c2 is better than c1; or 0 if neither is better.
		/// </summary>
		int BetterFunctionMember(Candidate c1, Candidate c2)
		{
			// prefer applicable members (part of heuristic that produces a best candidate even if none is applicable)
			if(c1.ErrorCount == 0 && c2.ErrorCount > 0)
				return 1;
			if(c1.ErrorCount > 0 && c2.ErrorCount == 0)
				return 2;
			
			// C# 4.0 spec: §7.5.3.2 Better function member
			bool c1_is_better = false;
			bool c2_is_better = false;
			for(int i = 0; i < arguments.Length; ++i){
				int p1 = c1.ArgumentToParameterMap[i];
				int p2 = c2.ArgumentToParameterMap[i];
				if(p1 >= 0 && p2 < 0){
					c1_is_better = true;
				}else if (p1 < 0 && p2 >= 0){
					c2_is_better = true;
				}else if (p1 >= 0 && p2 >= 0){
					switch (conversions.BetterConversion(arguments[i], c1.ParameterTypes[p1], c2.ParameterTypes[p2])) {
					case 1:
						c1_is_better = true;
						break;
					case 2:
						c2_is_better = true;
						break;
					}
				}
			}
			if (c1_is_better && !c2_is_better)
				return 1;
			if (!c1_is_better && c2_is_better)
				return 2;
			
			// prefer members with less errors (part of heuristic that produces a best candidate even if none is applicable)
			if (c1.ErrorCount < c2.ErrorCount) return 1;
			if (c1.ErrorCount > c2.ErrorCount) return 2;
			
			if (!c1_is_better && !c2_is_better) {
				// we need the tie-breaking rules
				
				// non-generic methods are better
				if (!c1.IsGenericMethod && c2.IsGenericMethod)
					return 1;
				else if (c1.IsGenericMethod && !c2.IsGenericMethod)
					return 2;
				
				// non-expanded members are better
				if (!c1.IsExpandedForm && c2.IsExpandedForm)
					return 1;
				else if (c1.IsExpandedForm && !c2.IsExpandedForm)
					return 2;
				
				// prefer the member with less arguments mapped to the params-array
				int r = c1.ArgumentsPassedToParamsArray.CompareTo(c2.ArgumentsPassedToParamsArray);
				if (r < 0) return 1;
				else if (r > 0) return 2;
				
				// prefer the member where no default values need to be substituted
				if (!c1.HasUnmappedOptionalParameters && c2.HasUnmappedOptionalParameters)
					return 1;
				else if (c1.HasUnmappedOptionalParameters && !c2.HasUnmappedOptionalParameters)
					return 2;
				
				// compare the formal parameters
				r = MoreSpecificFormalParameters(c1, c2);
				if (r != 0)
					return r;
				
				// prefer non-lifted operators
				ILiftedOperator lift1 = c1.Member as ILiftedOperator;
				ILiftedOperator lift2 = c2.Member as ILiftedOperator;
				if (lift1 == null && lift2 != null)
					return 1;
				if (lift1 != null && lift2 == null)
					return 2;
			}
			return 0;
		}
		
		/// <summary>
		/// Implement this interface to give overload resolution a hint that the member represents a lifted operator,
		/// which is used in the tie-breaking rules.
		/// </summary>
		public interface ILiftedOperator : IParameterizedMember
		{
			IList<IParameter> NonLiftedParameters { get; }
		}
		
		int MoreSpecificFormalParameters(Candidate c1, Candidate c2)
		{
			// prefer the member with more formal parmeters (in case both have different number of optional parameters)
			int r = c1.Parameters.Count.CompareTo(c2.Parameters.Count);
			if (r > 0) return 1;
			else if (r < 0) return 2;
			
			return MoreSpecificFormalParameters(c1.Parameters.Select(p => p.Type), c2.Parameters.Select(p => p.Type));
		}
		
		static int MoreSpecificFormalParameters(IEnumerable<IType> t1, IEnumerable<IType> t2)
		{
			bool c1IsBetter = false;
			bool c2IsBetter = false;
			foreach (var pair in t1.Zip(t2, (a,b) => new { Item1 = a, Item2 = b })) {
				switch (MoreSpecificFormalParameter(pair.Item1, pair.Item2)) {
				case 1:
					c1IsBetter = true;
					break;
				case 2:
					c2IsBetter = true;
					break;
				}
			}
			if (c1IsBetter && !c2IsBetter)
				return 1;
			if (!c1IsBetter && c2IsBetter)
				return 2;
			return 0;
		}
		
		static int MoreSpecificFormalParameter(IType t1, IType t2)
		{
			if ((t1 is ITypeParameter) && !(t2 is ITypeParameter))
				return 2;
			if ((t2 is ITypeParameter) && !(t1 is ITypeParameter))
				return 1;
			
			ParameterizedType p1 = t1 as ParameterizedType;
			ParameterizedType p2 = t2 as ParameterizedType;
			if (p1 != null && p2 != null && p1.TypeParameterCount == p2.TypeParameterCount) {
				int r = MoreSpecificFormalParameters(p1.TypeArguments, p2.TypeArguments);
				if (r > 0)
					return r;
			}
			TypeWithElementType tew1 = t1 as TypeWithElementType;
			TypeWithElementType tew2 = t2 as TypeWithElementType;
			if (tew1 != null && tew2 != null) {
				return MoreSpecificFormalParameter(tew1.ElementType, tew2.ElementType);
			}
			return 0;
		}
        #endregion*/
		
		#region ConsiderIfNewCandidateIsBest
		void ConsiderIfNewCandidateIsBest(Candidate candidate)
		{
			if(best_candidate == null){
				best_candidate = candidate;
				best_candidate_was_validated = false;
			}else{
				/*switch(/*BetterFunctionMember(candidate, best_candidate)1){
				case 0:
					// Overwrite 'bestCandidateAmbiguousWith' so that API users can
					// detect the set of all ambiguous methods if they look at
					// bestCandidateAmbiguousWith after each step.
					best_candidate_ambiguous_with = candidate;
					break;
				case 1:*/
					best_candidate = candidate;
					best_candidate_was_validated = false;
					best_candidate_ambiguous_with = null;
					/*break;
					// case 2: best candidate stays best
				}*/
			}
		}
        #endregion
		
		#region Output Properties
		public IParameterizedMember BestCandidate {
			get { return best_candidate != null ? best_candidate.Member : null; }
		}
		
		/// <summary>
		/// Returns the errors that apply to the best candidate.
		/// This includes additional errors that do not affect applicability (e.g. AmbiguousMatch, MethodConstraintsNotSatisfied)
		/// </summary>
		public OverloadResolutionErrors BestCandidateErrors {
			get {
				if(best_candidate == null)
					return OverloadResolutionErrors.None;
				
                if(!best_candidate_was_validated){
					best_candidate_validation_result = OverloadResolutionErrors.None;
					best_candidate_was_validated = true;
				}
				OverloadResolutionErrors err = best_candidate.Errors | best_candidate_validation_result;
				if (best_candidate_ambiguous_with != null)
					err |= OverloadResolutionErrors.AmbiguousMatch;
				return err;
			}
		}
		
		public bool FoundApplicableCandidate {
			get { return best_candidate != null && IsApplicable(best_candidate.Errors); }
		}
		
		public IParameterizedMember BestCandidateAmbiguousWith {
			get { return best_candidate_ambiguous_with != null ? best_candidate_ambiguous_with.Member : null; }
		}
		
		public bool BestCandidateIsExpandedForm {
			get { return best_candidate != null ? best_candidate.IsExpandedForm : false; }
		}
		
		public bool IsAmbiguous {
			get { return best_candidate_ambiguous_with != null; }
		}
		
		/// <summary>
		/// Gets an array that maps argument indices to parameter indices.
		/// For arguments that could not be mapped to any parameter, the value will be -1.
		/// 
		/// parameterIndex = GetArgumentToParameterMap()[argumentIndex]
		/// </summary>
		public IList<int> GetArgumentToParameterMap()
		{
			if (best_candidate != null)
				return best_candidate.ArgumentToParameterMap;
			else
				return null;
		}
		
		/// <summary>
		/// Creates a ResolveResult representing the result of overload resolution.
		/// </summary>
		/// <param name="targetResolveResult">
		/// The target expression of the call. May be <c>null</c> for static methods/constructors.
		/// </param>
		/// <param name="initializerStatements">
		/// Statements for Objects/Collections initializer.
		/// <see cref="InvocationResolveResult.InitializerStatements"/>
		/// </param>
		public InvocationResolveResult CreateResolveResult(ResolveResult targetResolveResult)
		{
			IParameterizedMember member = best_candidate.Member;
			if(member == null)
				throw new InvalidOperationException();
			
			return new InvocationResolveResult(targetResolveResult, member, arguments);
		}
        #endregion
	}
}

