using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.TypeCheck.Types;

namespace CobraCompiler.Assemble.ExpressionAssemblyContexts
{
    class ParentExpressionAssemblyContext
    {
        public readonly bool ImmediatelyCalling;
        public readonly CobraType ExpectedType;

        public ParentExpressionAssemblyContext(bool calling=false, CobraType expected=null)
        {
            ImmediatelyCalling = calling;
            ExpectedType = expected;
        }
    }
}
