using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class InvalidNullableAccessException: TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }
        public override bool isWarning => false;

        public InvalidNullableAccessException(MemberAccessExpression expr) : base($"Invalid null safe member access on non nullable type {expr.Obj.Type}.")
        {
            FirstToken = expr.FirstToken;
            LastToken = expr.LastToken;
        }
    }
}
