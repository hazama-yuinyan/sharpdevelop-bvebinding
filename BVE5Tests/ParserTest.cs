using System;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;

using BVE5Language.Ast;
using BVE5Language.Parser;

namespace BVE5Language.Ast
{
	class TypeDescriber
	{
		private readonly NodeType expected_type;
		private readonly List<TypeDescriber> children;
		
		public NodeType ExpectedType{
			get{return expected_type;}
		}
		
		public List<TypeDescriber> Children{
			get{return children;}
		}
		
		public TypeDescriber(NodeType targetType, List<TypeDescriber> inputChildren)
		{
			expected_type = targetType;
			children = inputChildren;
		}
		
		public static TypeDescriber Create(NodeType type, List<TypeDescriber> childDescirbers)
		{
			return new TypeDescriber(type, childDescirbers ?? new List<TypeDescriber>());
		}
	}

	internal static class Helpers
	{
		public static void AssertType(NodeType expected, NodeType actual)
		{
			Assert.IsTrue(actual == expected,
			              "Expected the node of type {0} but actually it is of {1}", expected, actual);
		}
		
		public static void TestStructualEqual(IEnumerator<TypeDescriber> expected, AstNode node)
        {
            bool expcted_has_node = expected.MoveNext();
            Assert.AreEqual(expcted_has_node, node != null);  //Tests whether both expected and node are not null or are null
            if(!expcted_has_node || node == null) return;

            var describer = expected.Current;
            AssertType(describer.ExpectedType, node.Type);
            foreach(var sibling in node.Siblings){
                TestStructualEqual(expected.Current.Children.GetEnumerator(), sibling.FirstChild);
                bool expected_has_next = expected.MoveNext();
                if(sibling.NextSibling != null && !expected_has_next)
                    Assert.Fail("Unexpected node found!");
                else if(sibling.NextSibling == null && expected_has_next)
                    Assert.Fail("Expected the current node to have a sibling node, but it doesn't.");
            }
        }
	}

	[TestFixture]
	public class ParserTest
	{
		[TestCase]
		public void Basics()
		{
            var parser = new BVE5RouteFileParser();
            var stmt = parser.ParseOneStatement("Sound.Load(sounds.txt);");
            var expected1 = new List<TypeDescriber>{
				TypeDescriber.Create(NodeType.Statement, new List<TypeDescriber>{
					TypeDescriber.Create(NodeType.Invocation, new List<TypeDescriber>{
						TypeDescriber.Create(NodeType.MemRef, new List<TypeDescriber>{
							TypeDescriber.Create(NodeType.Identifier, null),
							TypeDescriber.Create(NodeType.Identifier, null)
						}),
                        TypeDescriber.Create(NodeType.Identifier, null)
					})
				})
			};
            Helpers.TestStructualEqual(expected1.GetEnumerator(), stmt);

            var stmt2 = parser.ParseOneStatement("1000;");
            var expected2 = new List<TypeDescriber>{
                TypeDescriber.Create(NodeType.Statement, new List<TypeDescriber>{
                    TypeDescriber.Create(NodeType.Literal, null)
                })
            };
            Helpers.TestStructualEqual(expected2.GetEnumerator(), stmt2);

            var stmt3 = parser.ParseOneStatement("Track[Rail1].Position(9.7, 0, 300, 0);");
            var expected3 = new List<TypeDescriber>{
                TypeDescriber.Create(NodeType.Statement, new List<TypeDescriber>{
                    TypeDescriber.Create(NodeType.Invocation, new List<TypeDescriber>{
                        TypeDescriber.Create(NodeType.MemRef, new List<TypeDescriber>{
                            TypeDescriber.Create(NodeType.Indexer, new List<TypeDescriber>{
                                TypeDescriber.Create(NodeType.Identifier, null),
                                TypeDescriber.Create(NodeType.Identifier, null)
                            }),
                            TypeDescriber.Create(NodeType.Identifier, null)
                        }),
                        TypeDescriber.Create(NodeType.Literal, null),   //arguments
                        TypeDescriber.Create(NodeType.Literal, null),
                        TypeDescriber.Create(NodeType.Literal, null),
                        TypeDescriber.Create(NodeType.Literal, null)
                    })
                })
            };
            Helpers.TestStructualEqual(expected3.GetEnumerator(), stmt3);
		}
	}
}

