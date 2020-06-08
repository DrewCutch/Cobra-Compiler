using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.Assemble.ExpressionAssemblyContexts
{
    abstract class MethodExpressionAssemblyContext: ExpressionAssemblyContext
    {
        protected MethodExpressionAssemblyContext(CobraType type) : base(type)
        {
        }
    }
}
