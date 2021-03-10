using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class InvalidMemberAccessOnNullableException: TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }
        public override bool isWarning => false;

        public InvalidMemberAccessOnNullableException(GetExpression expr) : base($"Cannot access member {expr.Name.Lexeme} on nullable type {expr.Obj.Type.Identifier}")
        {
            FirstToken = expr.FirstToken;
            LastToken = expr.LastToken;
        }
    }
}
