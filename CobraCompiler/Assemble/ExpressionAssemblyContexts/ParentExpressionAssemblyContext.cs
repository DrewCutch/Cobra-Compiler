using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.Assemble.ExpressionAssemblyContexts
{
    class ParentExpressionAssemblyContext
    {
        public readonly bool ImmediatelyCalling;
        public readonly CobraType ExpectedType;
        public readonly bool Assigning;

        public ParentExpressionAssemblyContext(bool calling=false, CobraType expected=null, bool assigning=false)
        {
            ImmediatelyCalling = calling;
            ExpectedType = expected;
            Assigning = assigning;
        }
    }
}
