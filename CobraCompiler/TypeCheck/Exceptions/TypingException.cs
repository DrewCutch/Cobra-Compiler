using CobraCompiler.ErrorLogging;

namespace CobraCompiler.TypeCheck.Exceptions
{
    abstract class TypingException : CompilingException
    {
        public override int LineNumber { get; }
        protected TypingException(string message, int lineNumber): base(message)
        {
            LineNumber = lineNumber;
        }
    }
}
