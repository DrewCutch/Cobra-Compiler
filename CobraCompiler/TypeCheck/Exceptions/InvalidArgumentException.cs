using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class InvalidArgumentException: TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }

        public override bool isWarning => false;

        public InvalidArgumentException(Expression argument, string expectedType) : base($"Invalid argument of type {argument.Type}, expected {expectedType}")
        {
            FirstToken = argument.FirstToken;
            LastToken = argument.LastToken;
        }
    }
}
