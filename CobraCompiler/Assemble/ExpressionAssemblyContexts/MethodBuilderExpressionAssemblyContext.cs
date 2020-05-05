using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.TypeCheck;
using CobraCompiler.Parse.TypeCheck.Types;

namespace CobraCompiler.Assemble.ExpressionAssemblyContexts
{
    class MethodBuilderExpressionAssemblyContext: MethodExpressionAssemblyContext
    {
        public readonly MethodBase Method;
        public MethodBuilderExpressionAssemblyContext(CobraType type, MethodBase method) : base(type)
        {
            Method = method;
        }
    }
}
