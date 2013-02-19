using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BVE5Language.Ast;

using ICSharpCode.NRefactory.Semantics;

namespace BVE5Language.Resolver
{
    /// <summary>
    /// <see cref="IResolveVisitorNavigator"/> implementation that resolves a list of nodes.
    /// We will skip all nodes which are not the target nodes or ancestors of the target nodes.
    /// </summary>
    public class NodeListResolveVisitorNavigator : IResolveVisitorNavigator
    {
        readonly Dictionary<AstNode, ResolveVisitorNavigationMode> dict = new Dictionary<AstNode, ResolveVisitorNavigationMode>();

        /// <summary>
        /// Creates a new NodeListResolveVisitorNavigator that resolves the specified nodes.
        /// </summary>
        public NodeListResolveVisitorNavigator(params AstNode[] nodes)
            : this((IEnumerable<AstNode>)nodes)
        {
        }

        /// <summary>
        /// Creates a new NodeListResolveVisitorNavigator that resolves the specified nodes.
        /// </summary>
        public NodeListResolveVisitorNavigator(IEnumerable<AstNode> nodes, bool scanOnly = false)
        {
            if(nodes == null)
                throw new ArgumentNullException("nodes");
            
            foreach(var node in nodes){
                dict[node] = scanOnly ? ResolveVisitorNavigationMode.Scan : ResolveVisitorNavigationMode.Resolve;
                for(var ancestor = node.Parent; ancestor != null && !dict.ContainsKey(ancestor); ancestor = ancestor.Parent)
                    dict.Add(ancestor, ResolveVisitorNavigationMode.Scan);
            }
        }

        /// <inheritdoc/>
        public virtual ResolveVisitorNavigationMode Scan(AstNode node)
        {
            ResolveVisitorNavigationMode mode;
            if(dict.TryGetValue(node, out mode))
                return mode;
            else
                return ResolveVisitorNavigationMode.Skip;
        }

        public virtual void Resolved(AstNode node, ResolveResult result)
        {
        }
    }
}
