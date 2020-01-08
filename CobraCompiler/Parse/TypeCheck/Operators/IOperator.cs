using System;
using CobraCompiler.Parse.TypeCheck.Types;

namespace CobraCompiler.Parse.TypeCheck.Operators
{
    interface IOperator
    { 
        TokenType OperatorToken { get; }
        CobraType ResultType { get; }
    }
}
