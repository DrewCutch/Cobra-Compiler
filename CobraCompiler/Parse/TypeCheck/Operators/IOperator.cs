using System;
using CobraCompiler.Parse.TypeCheck.Types;

namespace CobraCompiler.Parse.TypeCheck.Operators
{
    interface IOperator
    { 
        Operation Operation{ get; }
        CobraType ResultType { get; }
    }
}
