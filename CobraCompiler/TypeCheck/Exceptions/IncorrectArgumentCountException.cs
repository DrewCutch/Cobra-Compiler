using System.Collections.Generic;
using System.Linq;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class IncorrectArgumentCountException: TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }

        public override bool isWarning => false;

        public IncorrectArgumentCountException(CallExpression callExpression, int expectedArgs) : base($"Function expects {expectedArgs} arguments but is provided {callExpression.Arguments.Count}")
        {
            if (callExpression.Arguments.Count == 0)
            {
                FirstToken = callExpression.ClosingParen.Previous;
                LastToken = callExpression.ClosingParen;
            }
            else
            {
                FirstToken = callExpression.Arguments.First().FirstToken;
                LastToken = callExpression.Arguments.Last().LastToken;
            }
        }
    }
}
