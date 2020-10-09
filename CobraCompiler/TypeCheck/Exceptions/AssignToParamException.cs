using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class AssignToParamException: TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }

        public override bool isWarning => false;
        public AssignToParamException(AssignExpression assignExpression, Symbol paramSymbol) : base($"Cannot assign to {paramSymbol.Lexeme} because it is a parameter")
        {
            FirstToken = assignExpression.FirstToken;
            LastToken = assignExpression.LastToken;
        }
    }
}
