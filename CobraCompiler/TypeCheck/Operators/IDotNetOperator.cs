using System.Reflection.Emit;

namespace CobraCompiler.TypeCheck.Operators
{
    interface IDotNetOperator
    {
        OpCode OpCode { get; }
    }
}
