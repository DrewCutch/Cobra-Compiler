using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.TypeCheck.Operators
{
    interface IOperator
    { 
        Operation Operation{ get; }
        CobraType ResultType { get; }
    }
}
