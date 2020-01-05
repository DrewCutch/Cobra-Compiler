using System.Reflection.Emit;

namespace CobraCompiler.Parse.TypeCheck.Operators
{
    interface IDotNetOperator
    {
        OpCode OpCode { get; }
    }
}
