using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace BVE5Language.Semantics
{
    public class PositionStatementResolveResult : ResolveResult
    {
        private readonly int position_value;

        public PositionStatementResolveResult(int position, IType integerType) : base(integerType)
        {
            position_value = position;
        }

        public override object ConstantValue{
            get{
                return position_value;
            }
        }

        public override bool IsCompileTimeConstant{
            get{
                return true;
            }
        }

        public override string ToString()
        {
            return "[PositionStatement pos: " + position_value + "]";
        }
    }
}
