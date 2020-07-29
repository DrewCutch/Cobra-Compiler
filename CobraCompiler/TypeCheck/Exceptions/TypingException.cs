using CobraCompiler.ErrorLogging;
using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    abstract class TypingException : CompilingException
    {
        protected TypingException(string message): base(message)
        {
        }
    }
}
