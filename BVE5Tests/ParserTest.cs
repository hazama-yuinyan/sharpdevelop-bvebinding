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
		readonly NodeType expected_type;
		readonly List<TypeDescriber> children;
		
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
	public class RouteParserTest
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
                                TypeDescriber.Create(NodeType.Literal, null)
                            }),
                            TypeDescriber.Create(NodeType.Identifier, null)
                        }),
                        TypeDescriber.Create(NodeType.Literal, null),   //9.7
                        TypeDescriber.Create(NodeType.Literal, null),	//0
                        TypeDescriber.Create(NodeType.Literal, null),	//300
                        TypeDescriber.Create(NodeType.Literal, null)	//0
                    })
                })
            };
            Helpers.TestStructualEqual(expected3.GetEnumerator(), stmt3);
            
            var tree = parser.Parse(@"Track[1].Position(0, 0, 100, 0);
//This is a comment
Track[2].Position(5.4, 0, 100, 0);",
                                    "<string>");
            var expected4 = new List<TypeDescriber>{
            	TypeDescriber.Create(NodeType.Tree, new List<TypeDescriber>{
            		TypeDescriber.Create(NodeType.Statement, new List<TypeDescriber>{
            	    	TypeDescriber.Create(NodeType.Invocation, new List<TypeDescriber>{
            	        	TypeDescriber.Create(NodeType.MemRef, new List<TypeDescriber>{
            	            	TypeDescriber.Create(NodeType.Indexer, new List<TypeDescriber>{
            	                	TypeDescriber.Create(NodeType.Identifier, null),
            	                    TypeDescriber.Create(NodeType.Literal, null)
            	                }),
            	                TypeDescriber.Create(NodeType.Identifier, null)
            	            }),
            	            TypeDescriber.Create(NodeType.Literal, null),	//0
            	            TypeDescriber.Create(NodeType.Literal, null),	//0
            	            TypeDescriber.Create(NodeType.Literal, null),	//100
            	            TypeDescriber.Create(NodeType.Literal, null)	//0
            	        })
            	    }),
            	    TypeDescriber.Create(NodeType.Statement, new List<TypeDescriber>{
            	    	TypeDescriber.Create(NodeType.Invocation, new List<TypeDescriber>{
            	        	TypeDescriber.Create(NodeType.MemRef, new List<TypeDescriber>{
            	            	TypeDescriber.Create(NodeType.Indexer, new List<TypeDescriber>{
            	                	TypeDescriber.Create(NodeType.Identifier, null),
            	                    TypeDescriber.Create(NodeType.Literal, null)
            	                }),
            	                TypeDescriber.Create(NodeType.Identifier, null)
            	            }),
            	            TypeDescriber.Create(NodeType.Literal, null),	//5.4
            	            TypeDescriber.Create(NodeType.Literal, null),	//0
            	            TypeDescriber.Create(NodeType.Literal, null),	//100
            	            TypeDescriber.Create(NodeType.Literal, null)	//0
            	        })
            	    })
            	})
            };
            Helpers.TestStructualEqual(expected4.GetEnumerator(), tree);
		}
		
		[TestCase]
		public void Additions()
		{
			var parser = new BVE5RouteFileParser();
			var stmt = parser.ParseOneStatement("let a = 1;");
			var expected1 = new List<TypeDescriber>{
				TypeDescriber.Create(NodeType.Statement, new List<TypeDescriber>{
					TypeDescriber.Create(NodeType.Definition, new List<TypeDescriber>{
				    	TypeDescriber.Create(NodeType.Identifier, null),
				        TypeDescriber.Create(NodeType.Literal, null)
				    })
				})
			};
			Helpers.TestStructualEqual(expected1.GetEnumerator(), stmt);
			
			var stmt2 = parser.ParseOneStatement("let ‰½‚© = 1;");
			var expected2 = new List<TypeDescriber>{
				TypeDescriber.Create(NodeType.Statement, new List<TypeDescriber>{
					TypeDescriber.Create(NodeType.Definition, new List<TypeDescriber>{
				    	TypeDescriber.Create(NodeType.Identifier, null),
				        TypeDescriber.Create(NodeType.Literal, null)
				    })
				})
			};
			Helpers.TestStructualEqual(expected2.GetEnumerator(), stmt2);
		}
	}
	
	[TestFixture]
	public class CommonParserTest
	{
		[TestCase]
		public void CommonParser()
		{
			var parser = new BVE5CommonParser("BveTs Station List 1.01", "Station list");
			var stmt = parser.ParseOneStatement("staA, A, 10:30:00, 10:30:30, 20, 10:30:00, 0, 10, 0.3, soundStaA, soundStaADeperture, 0.05, 5\n");
			var expected1 = new List<TypeDescriber>{
				TypeDescriber.Create(NodeType.Statement, new List<TypeDescriber>{
					TypeDescriber.Create(NodeType.Sequence, new List<TypeDescriber>{
				    	TypeDescriber.Create(NodeType.Literal, null),		//staA
				        TypeDescriber.Create(NodeType.Literal, null),		//A
				        TypeDescriber.Create(NodeType.TimeLiteral, null),	//10:30:00
				        TypeDescriber.Create(NodeType.TimeLiteral, null),	//10:30:30
				        TypeDescriber.Create(NodeType.Literal, null),		//20
				        TypeDescriber.Create(NodeType.TimeLiteral, null),	//10:30:00
				        TypeDescriber.Create(NodeType.Literal, null),		//0
				        TypeDescriber.Create(NodeType.Literal, null),		//10
				        TypeDescriber.Create(NodeType.Literal, null),		//0.3
				        TypeDescriber.Create(NodeType.Literal, null),		//soundStaA
				        TypeDescriber.Create(NodeType.Literal, null),		//soundStaADeperture
				        TypeDescriber.Create(NodeType.Literal, null),		//0.05
				        TypeDescriber.Create(NodeType.Literal, null)		//5
				    })
				})
			};
			Helpers.TestStructualEqual(expected1.GetEnumerator(), stmt);
		}
	}
	
	[TestFixture]
	public class InitFileParserTest
	{
		[TestCase]
		public void InitFileParser()
		{
			var parser = new InitFileParser("BveTs Vehicle Parameters 1.01", "Vehicle parameters");
			var stmt = parser.ParseOneStatement("[Cab]");
			var expected1 = new List<TypeDescriber>{
				TypeDescriber.Create(NodeType.SectionStmt, new List<TypeDescriber>{
					TypeDescriber.Create(NodeType.Identifier, null)
				})
			};
			Helpers.TestStructualEqual(expected1.GetEnumerator(), stmt);
			
			var stmt2 = parser.ParseOneStatement("driver = 100\n");
			var expected2 = new List<TypeDescriber>{
				TypeDescriber.Create(NodeType.Statement, new List<TypeDescriber>{
					TypeDescriber.Create(NodeType.Definition, new List<TypeDescriber>{
				    	TypeDescriber.Create(NodeType.Identifier, null),
				        TypeDescriber.Create(NodeType.Literal, null)
				    })
				})
			};
			Helpers.TestStructualEqual(expected2.GetEnumerator(), stmt2);
			
			var stmt3 = parser.Parse(@"color = #33ffbb
;This is a comment
MotorcarCount = 4
DayTimeImage = imgs\test.png",
			                         "<string>");
			var expected3 = new List<TypeDescriber>{
				TypeDescriber.Create(NodeType.Tree, new List<TypeDescriber>{
					TypeDescriber.Create(NodeType.Statement, new List<TypeDescriber>{
						TypeDescriber.Create(NodeType.Definition, new List<TypeDescriber>{
					    	TypeDescriber.Create(NodeType.Identifier, null),
					        TypeDescriber.Create(NodeType.Literal, null)
					    })
					}),
					TypeDescriber.Create(NodeType.Statement, new List<TypeDescriber>{
						TypeDescriber.Create(NodeType.Definition, new List<TypeDescriber>{
					    	TypeDescriber.Create(NodeType.Identifier, null),
					        TypeDescriber.Create(NodeType.Literal, null)
					    })
					}),
				    TypeDescriber.Create(NodeType.Statement, new List<TypeDescriber>{
				    	TypeDescriber.Create(NodeType.Definition, new List<TypeDescriber>{
				        	TypeDescriber.Create(NodeType.Identifier, null),
				            TypeDescriber.Create(NodeType.Literal, null)
				        })
				    })
				})
			};
			Helpers.TestStructualEqual(expected3.GetEnumerator(), stmt3);
		}
	}
}

