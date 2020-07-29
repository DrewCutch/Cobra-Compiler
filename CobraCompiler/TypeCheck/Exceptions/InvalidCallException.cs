using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class InvalidCallException: TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }
        public override bool isWarning => false;
        public InvalidCallException(CallExpression callExpression) : base($"Cannot call instance of {callExpression.Callee.Type}")
        {
            FirstToken = callExpression.FirstToken;
            LastToken = callExpression.LastToken;
        }
    }
}
