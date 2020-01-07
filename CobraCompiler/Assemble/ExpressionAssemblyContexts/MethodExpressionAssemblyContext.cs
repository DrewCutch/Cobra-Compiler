using CobraCompiler.Parse.TypeCheck;
using CobraCompiler.Parse.TypeCheck.Types;

namespace CobraCompiler.Assemble.ExpressionAssemblyContexts
{
    abstract class MethodExpressionAssemblyContext: ExpressionAssemblyContext
    {
        protected MethodExpressionAssemblyContext(CobraType type) : base(type)
        {
        }
    }
}
