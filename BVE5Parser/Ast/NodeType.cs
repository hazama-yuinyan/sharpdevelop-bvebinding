using System;

namespace BVE5Language.Ast
{
	public enum NodeType
	{
		None,
		Expression,
		Identifier,
		Indexer,
		Invocation,
		Literal,
		MemRef,
		Statement,
		Tree,
		TimeLiteral,
		Definition,
		SectionStmt,
		Sequence,
		BinaryExpression,
		UnaryExpression
	}
}

