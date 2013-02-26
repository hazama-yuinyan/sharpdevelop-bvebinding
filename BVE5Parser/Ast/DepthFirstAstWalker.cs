using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BVE5Language.Ast
{
    /// <summary>
    /// AST walker with a default implementation that walks over all node depth-first.
    /// </summary>
    public abstract class DepthFirstAstWalker<T> : IAstWalker<T>
    {
        protected virtual T WalkChildren(AstNode node)
        {
            AstNode next;
            for(var child = node.FirstChild; child != null; child = next){
                // Store next to allow the loop to continue
                // if the visitor removes/replaces child.
                next = child.NextSibling;
                child.AcceptWalker(this);
            }
            return default(T);
        }

        #region IAstWalker<T> members
        public virtual T Walk(DefinitionExpression def)
        {
        	return WalkChildren(def);
        }
        
        public virtual T Walk(Identifier node)
        {
            return WalkChildren(node);
        }

        public virtual T Walk(IndexerExpression node)
        {
            return WalkChildren(node);
        }

        public virtual T Walk(InvocationExpression node)
        {
            return WalkChildren(node);
        }

        public virtual T Walk(LiteralExpression node)
        {
            return WalkChildren(node);
        }

        public virtual T Walk(MemberReferenceExpression node)
        {
            return WalkChildren(node);
        }
        
        public virtual T Walk(SectionStatement secStmt)
        {
        	return WalkChildren(secStmt);
        }
        
        public virtual T Walk(SequenceExpression sequence)
        {
        	return WalkChildren(sequence);
        }

        public virtual T Walk(Statement node)
        {
            return WalkChildren(node);
        }

        public virtual T Walk(SyntaxTree node)
        {
            return WalkChildren(node);
        }

        public virtual T Walk(TimeFormatLiteral node)
        {
            return WalkChildren(node);
        }
        #endregion
    }
}
