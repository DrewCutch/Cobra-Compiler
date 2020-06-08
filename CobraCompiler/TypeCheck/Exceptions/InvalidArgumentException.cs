using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class InvalidArgumentException: TypingException
    {
        public override bool isWarning => false;

        public InvalidArgumentException(Token token, string expectedType, string type) : base($"Invalid argument of type {type}, expected {expectedType}", token.Line)
        {
        }
    }
}
