using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.TypeCheck.Exceptions
{
    class IncorrectArgumentCountException: TypingException
    {
        public override bool isWarning => false;

        public IncorrectArgumentCountException(Token token, int expectedArgs, int providedArgs) : base($"Function expects {expectedArgs} arguments but is provided {providedArgs}", token.Line)
        {

        }
    }
}
