using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BVE5Language.Ast;

using ICSharpCode.NRefactory.Semantics;

namespace BVE5Language.Resolver
{
    /// <summary>
    /// Allows controlling which nodes are resolved by the resolve visitor.
    /// </summary>
    /// <seealso cref="ResolveVisitor"/>
    public interface IResolveVisitorNavigator
    {
        /// <summary>
        /// Asks the navigator whether to scan, skip, or resolve a node.
        /// </summary>
        ResolveVisitorNavigationMode Scan(AstNode node);

        /// <summary>
        /// Notifies the navigator that a node was resolved.
        /// </summary>
        /// <param name="node">The node that was resolved</param>
        /// <param name="result">Resolve result</param>
        void Resolved(AstNode node, ResolveResult result);
    }

    /// <summary>
    /// Represents the operation mode of the resolve visitor.
    /// </summary>
    /// <seealso cref="ResolveVisitor"/>
    public enum ResolveVisitorNavigationMode
    {
        /// <summary>
        /// Scan into the children of the current node, without resolving the current node.
        /// </summary>
        Scan,
        /// <summary>
        /// Skip the current node - do not scan into it; do not resolve it.
        /// </summary>
        Skip,
        /// <summary>
        /// Resolve the current node.
        /// Subnodes which are not required for resolving the current node
        /// will ask the navigator again whether they should be resolved.
        /// </summary>
        Resolve
    }

    sealed class ConstantModeResolveVisitorNavigator : IResolveVisitorNavigator
    {
        readonly ResolveVisitorNavigationMode mode;
        readonly IResolveVisitorNavigator target_for_resolve_calls;

        public ConstantModeResolveVisitorNavigator(ResolveVisitorNavigationMode mode, IResolveVisitorNavigator targetForResolveCalls)
        {
            this.mode = mode;
            this.target_for_resolve_calls = targetForResolveCalls;
        }

        ResolveVisitorNavigationMode IResolveVisitorNavigator.Scan(AstNode node)
        {
            return mode;
        }

        void IResolveVisitorNavigator.Resolved(AstNode node, ResolveResult result)
        {
            if(target_for_resolve_calls != null)
                target_for_resolve_calls.Resolved(node, result);
        }
    }
}
