//
// AstNode.cs
//
// Author:
//       HAZAMA <kotonechan@live.jp>
//
// Copyright (c) 2013 HAZAMA
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ICSharpCode.NRefactory;

namespace BVE5Language.Ast
{
	/// <summary>
	/// Base class for all the AST nodes.
	/// </summary>
	public abstract class AstNode
	{
		protected readonly TextLocation start_loc, end_loc;
		AstNode parent, prev_sibling, next_sibling, first_child, last_child;

		#region Properties
		public TextLocation StartLocation{
			get{return start_loc;}
		}

		public TextLocation EndLocation{
			get{return end_loc;}
		}

		public AstNode Parent{
			get{return parent;}
		}

		public AstNode NextSibling{
			get{return next_sibling;}
		}

		public AstNode PrevSibling{
			get{return prev_sibling;}
		}

		public AstNode FirstChild{
			get{return first_child;}
		}

		public AstNode LastChild{
			get{return last_child;}
		}

		/// <summary>
		/// Traverses each child node excluding this one.
		/// </summary>
		/// <value>
		/// The children.
		/// </value>
		public IEnumerable<AstNode> Children{
			get{
				AstNode node = first_child;
				while(node != null){
					yield return node;
					node = node.first_child;
				}
			}
		}

        /// <summary>
        /// Traverses each sibling node including this one.
        /// </summary>
		public IEnumerable<AstNode> Siblings{
			get{
				AstNode node = this;
				while(node != null){
					yield return node;
					node = node.next_sibling;
				}
			}
		}

		public abstract NodeType Type{
			get;
		}
		#endregion

		protected AstNode(TextLocation startLoc, TextLocation endLoc)
		{
			start_loc = startLoc;
			end_loc = endLoc;
		}

		/// <summary>
		/// Accepts the ast walker.
		/// </summary>
		/// <param name='walker'>
		/// Walker.
		/// </param>
		public abstract void AcceptWalker(AstWalker walker);
		public abstract TResult AcceptWalker<TResult>(IAstWalker<TResult> walker);
		/// <summary>
		/// Gets the text representation of the node.
		/// </summary>
		/// <returns>
		/// The text.
		/// </returns>
		public abstract string GetText();

		#region CommonAstNode methods
        /// <summary>
        /// Determines whether the specified location is inside this node.
        /// </summary>
        /// <param name="loc">The text location to test against</param>
        /// <returns>true, if the location is inside this node; otherwise false</returns>
		public bool Contains(TextLocation loc)
		{
			return start_loc < loc && loc < end_loc;
		}

        #region Node manipulation
        public void AddChild<T>(T child) where T : AstNode
		{
			if(child == null)
				return;

			if(child.parent != null)
				throw new ArgumentException("Node is already used in another tree.", "child");

			AddChildUnsafe(child);
		}
		
		/// <summary>
		/// Adds a child without performing any safety checks.
		/// </summary>
		void AddChildUnsafe(AstNode child)
		{
			child.parent = this;
			if(first_child == null){
				last_child = first_child = child;
			}else{
				last_child.next_sibling = child;
				child.prev_sibling = last_child;
				last_child = child;
			}
		}
		
        public void InsertChildBefore<T>(AstNode nextSibling, T child) where T : AstNode
        {
            if(nextSibling == null){
                AddChild(child);
                return;
            }

            if(child == null)
                return;

            if(child.parent != null)
                throw new ArgumentException("Node is already used in another tree.", "child");
            
            if(nextSibling.parent != this)
                throw new ArgumentException("NextSibling is not a child of this node.", "nextSibling");
            
            // No need to test for "Cannot add children to null nodes",
            // as there isn't any valid nextSibling in null nodes.
            InsertChildBeforeUnsafe(nextSibling, child);
        }

        void InsertChildBeforeUnsafe(AstNode nextSibling, AstNode child)
        {
            child.parent = this;
            child.next_sibling = nextSibling;
            child.prev_sibling = nextSibling.PrevSibling;

            if(nextSibling.PrevSibling != null){
                Debug.Assert(nextSibling.PrevSibling.NextSibling == nextSibling);
                nextSibling.PrevSibling.next_sibling = child;
            } else {
                Debug.Assert(FirstChild == nextSibling);
                first_child = child;
            }
            nextSibling.prev_sibling = child;
        }

        public void InsertChildAfter<T>(AstNode prevSibling, T child) where T : AstNode
        {
            InsertChildBefore((prevSibling == null) ? FirstChild : prevSibling.NextSibling, child);
        }

        /// <summary>
        /// Removes this node from its parent.
        /// </summary>
        public void Remove()
        {
            if(parent != null){
                if(PrevSibling != null){
                    Debug.Assert(PrevSibling.NextSibling == this);
                    PrevSibling.next_sibling = NextSibling;
                }else{
                    Debug.Assert(parent.FirstChild == this);
                    parent.first_child = NextSibling;
                }

                if(NextSibling != null){
                    Debug.Assert(NextSibling.PrevSibling == this);
                    NextSibling.prev_sibling = PrevSibling;
                }else{
                    Debug.Assert(parent.LastChild == this);
                    parent.last_child = PrevSibling;
                }

                parent = null;
                prev_sibling = null;
                next_sibling = null;
            }
        }

        /// <summary>
        /// Replaces this node with the new node.
        /// </summary>
        public void ReplaceWith(AstNode newNode)
        {
            if(newNode == null){
                Remove();
                return;
            }

            if(newNode == this)
                return; // nothing to do...
            
            if(parent == null)
                throw new InvalidOperationException("Cannot replace the root node");
            
            if(newNode.parent != null){
                // newNode is used within this tree?
                if(newNode.Ancestors.Contains(this)){
                    // e.g. "parenthesizedExpr.ReplaceWith(parenthesizedExpr.Expression);"
                    // enable automatic removal
                    newNode.Remove();
                }else{
                    throw new ArgumentException("Node is already used in another tree.", "newNode");
                }
            }

            newNode.parent = parent;
            newNode.prev_sibling = PrevSibling;
            newNode.next_sibling = NextSibling;

            if(PrevSibling != null){
                Debug.Assert(PrevSibling.NextSibling == this);
                PrevSibling.next_sibling = newNode;
            }else{
                Debug.Assert(parent.FirstChild == this);
                parent.first_child = newNode;
            }

            if(NextSibling != null){
                Debug.Assert(NextSibling.PrevSibling == this);
                NextSibling.prev_sibling = newNode;
            }else{
                Debug.Assert(parent.LastChild == this);
                parent.last_child = newNode;
            }

            parent = null;
            prev_sibling = null;
            next_sibling = null;
        }

        public AstNode ReplaceWith(Func<AstNode, AstNode> replaceFunction)
        {
            if(replaceFunction == null)
                throw new ArgumentNullException("replaceFunction");
            
            if(parent == null)
                throw new InvalidOperationException("Cannot replace the root node");
            
            AstNode old_parent = parent;
            AstNode old_successor = NextSibling;
            Remove();
            AstNode replacement = replaceFunction(this);
            if(old_successor != null && old_successor.parent != old_parent)
                throw new InvalidOperationException("replace function changed nextSibling of node being replaced?");
            
            if(replacement != null){
                if(replacement.parent != null)
                    throw new InvalidOperationException("replace function must return the root of a tree");
                
                if(old_successor != null)
                    old_parent.InsertChildBeforeUnsafe(old_successor, replacement);
                else
                    old_parent.AddChildUnsafe(replacement);
            }
            return replacement;
        }
        #endregion

        #region Traversing nodes
        /// <summary>
        /// Gets the ancestors of this node (excluding this node itself)
        /// </summary>
        public IEnumerable<AstNode> Ancestors
        {
            get{
                for(AstNode cur = parent; cur != null; cur = cur.parent)
                    yield return cur;
            }
        }

        /// <summary>
        /// Traverses child node that match the predicate.
        /// </summary>
        /// <param name="pred">A delegate that determines which nodes must be traversed and which must not.
        /// If it is null, this method will traverses all the children including this one.</param>
        public IEnumerable<AstNode> GetChildren(Predicate<AstNode> pred)
        {
            if(pred == null)
                pred = n => true;

            AstNode node = this;
            while(node != null){
                if(pred(node))
                    yield return node;

                node = node.first_child;
            }
        }
        
        /// <summary>
        /// Iterates over each child and tries to find the first match to the predicate.
        /// </summary>
        /// <param name="pred">A delegate that determines which node is searched for.</param>
        /// <returns>The found node, or null if none is found.</returns>
        /// <exception cref="InvalidOperationException">When multiple nodes match the predicate, it'll be thrown.</exception>
        public AstNode FindNode(Predicate<AstNode> pred)
        {
        	AstNode node = this, result = null;
        	var queue = new Queue<AstNode>();
        	while(node != null){
        		if(pred(node)){
        			if(result != null)
        				throw new InvalidOperationException("Multiple nodes found!");
        			else
        				result = node;
        		}
        		
        		if(node.next_sibling != null){
        			queue.Enqueue(node);
        			node = node.next_sibling;
        		}else{
        			var next_parent = queue.Any() ? queue.Dequeue() : node;
        			node = next_parent.first_child;
        		}
        	}
        	
        	return result;
        }
        
        /// <summary>
        /// Iterates over each node and finds all matches to the predicate.
        /// </summary>
        /// <remarks>
        /// This method traverses all the sibling and child nodes rooted at this one.
        /// </remarks>
        /// <param name="pred">A delegate that determines which node should be included in the result.</param>
        /// <returns>An IEnumerable that enumerates the matched nodes.</returns>
        public IEnumerable<AstNode> FindNodes(Predicate<AstNode> pred)
        {
        	AstNode node = this;
        	var queue = new Queue<AstNode>();
        	while(node != null){
        		if(pred(node))
        			yield return node;
        		
        		if(node.next_sibling != null){
        			queue.Enqueue(node);
        			node = node.next_sibling;
        		}else{
        			var next_parent = queue.Any() ? queue.Dequeue() : node;
        			node = next_parent.first_child;
        		}
        	}
        }
        #endregion

		#region GetNodeAt
		/// <summary>
		/// Gets the node specified by T at the location line, column. This is useful for getting a specific node from the tree. For example searching
		/// the current method declaration.
		/// (End exclusive)
		/// </summary>
		public AstNode GetNodeAt(int line, int column, Predicate<AstNode> pred = null)
		{
			return GetNodeAt(new TextLocation(line, column), pred);
		}
		
		/// <summary>
		/// Gets the node specified by pred at location. This is useful for getting a specific node from the tree. For example searching
		/// the current method declaration.
		/// (End exclusive)
		/// </summary>
		public AstNode GetNodeAt(TextLocation location, Predicate<AstNode> pred = null)
		{
			AstNode result = null;
			AstNode node = this;
			while(node.FirstChild != null){
				var child = node.FirstChild;
				while(child != null){
					if(child.StartLocation <= location && location < child.EndLocation){
						if(pred == null || pred (child))
							result = child;

						node = child;
						break;
					}
					child = child.NextSibling;
				}
				// found no better child node - therefore the parent is the right one.
				if(child == null)
					break;
			}
			return result;
		}
		
		/// <summary>
		/// Gets the node specified by T at the location line, column. This is useful for getting a specific node from the tree. For example searching
		/// the current method declaration.
		/// (End exclusive)
		/// </summary>
		public T GetNodeAt<T>(int line, int column) where T : AstNode
		{
			return GetNodeAt<T>(new TextLocation(line, column));
		}
		
		/// <summary>
		/// Gets the node specified by T at location. This is useful for getting a specific node from the tree. For example searching
		/// the current method declaration.
		/// (End exclusive)
		/// </summary>
		public T GetNodeAt<T>(TextLocation location) where T : AstNode
		{
			T result = null;
			AstNode node = this;
			while(node.FirstChild != null){
				var child = node.FirstChild;
				while(child != null){
					if(child.StartLocation <= location && location < child.EndLocation){
						if(child is T)
							result = (T)child;

						node = child;
						break;
					}
					child = child.NextSibling;
				}
				// found no better child node - therefore the parent is the right one.
				if(child == null)
					break;
			}
			return result;
		}
		#endregion
		#endregion

		#region Node factories
		internal static Identifier MakeIdent(string name, TextLocation start, TextLocation end)
		{
			return new Identifier(name, start, end);
		}

		internal static IndexerExpression MakeIndexExpr(Expression target, LiteralExpression key, TextLocation start, TextLocation end)
		{
			return new IndexerExpression(target, key, start, end);
		}

		internal static InvocationExpression MakeInvoke(Expression invokeTarget, List<Expression> args, TextLocation start,
		                                                TextLocation end)
		{
			return new InvocationExpression(invokeTarget, args, start, end);
		}

		internal static LiteralExpression MakeLiteral(object value, TextLocation start, TextLocation end)
		{
			return new LiteralExpression(value, start, end);
		}
		
		internal static DefinitionExpression MakeDefinition(Identifier lhs, Expression rhs, TextLocation start, TextLocation end)
		{
			return new DefinitionExpression(lhs, rhs, start, end);
		}

		internal static MemberReferenceExpression MakeMemRef(Expression target, Identifier reference, TextLocation start, TextLocation end)
		{
			return new MemberReferenceExpression(target, reference, start, end);
		}
		
		internal static SectionStatement MakeSectionStatement(Identifier ident, TextLocation start, TextLocation end)
		{
			return new SectionStatement(ident, start, end);
		}
		
		internal static SequenceExpression MakeSequence(List<Expression> exprs, TextLocation start, TextLocation end)
		{
			return new SequenceExpression(exprs, start, end);
		}

		internal static Statement MakeStatement(Expression expr, TextLocation start, TextLocation end)
		{
			return new Statement(expr, start, end);
		}

		internal static SyntaxTree MakeSyntaxTree(List<Statement> body, string name, TextLocation start, TextLocation end)
		{
			return new SyntaxTree(body, name, start, end);
		}

		internal static TimeFormatLiteral MakeTimeFormat(int hour, int min, int sec, TextLocation start, TextLocation end)
		{
			return new TimeFormatLiteral(hour, min, sec, start, end);
		}
		
		internal static BinaryExpression MakeBinary(Expression lhs, Expression rhs, Operator ope, TextLocation start, TextLocation end)
		{
			return new BinaryExpression(lhs, rhs, ope, start, end);
		}
		
		internal static UnaryExpression MakeUnary(Expression operand, Operator ope, TextLocation start, TextLocation end)
		{
			return new UnaryExpression(operand, ope, start, end);
		}
		#endregion
	}
}

