using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class InvalidReturnTypeException: TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }

        public override bool isWarning => false;
        public InvalidReturnTypeException(Expression returnExpr, CobraType expectedType) : base($"Cannot return value of type {returnExpr.Type} from function with {expectedType} return type.")
        {
            FirstToken = returnExpr.FirstToken;
            LastToken = returnExpr.LastToken;
        }
    }
}
