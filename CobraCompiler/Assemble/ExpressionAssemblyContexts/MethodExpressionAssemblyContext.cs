using CobraCompiler.Parse.TypeCheck;

namespace CobraCompiler.Assemble.ExpressionAssemblyContexts
{
    abstract class MethodExpressionAssemblyContext: ExpressionAssemblyContext
    {
        protected MethodExpressionAssemblyContext(CobraType type) : base(type)
        {
        }
    }
}
