using CobraCompiler.Parse.TypeCheck;

namespace CobraCompiler.Assemble.ExpressionAssemblyContexts
{
    class ExpressionAssemblyContext
    {
        public readonly CobraType Type;
        
        public ExpressionAssemblyContext(CobraType type)
        {
            Type = type;
        }
    }
}
