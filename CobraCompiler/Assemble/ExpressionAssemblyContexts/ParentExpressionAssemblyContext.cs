using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Assemble.ExpressionAssemblyContexts
{
    class ParentExpressionAssemblyContext
    {
        public readonly bool ImmediatelyCalling;

        public ParentExpressionAssemblyContext(bool immediatelyCalling)
        {
            ImmediatelyCalling = immediatelyCalling;
        }
    }
}
