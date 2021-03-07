using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class UnassignedVarException : TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }

        public override bool isWarning => false;
        public UnassignedVarException(VarExpression varExpression) : base($"Cannot reference {varExpression.Name.Lexeme} before it is assigned")
        {
            FirstToken = varExpression.FirstToken;
            LastToken = varExpression.LastToken;
        }
    }
}
