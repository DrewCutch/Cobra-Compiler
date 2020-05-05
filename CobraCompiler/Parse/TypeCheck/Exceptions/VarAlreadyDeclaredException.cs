using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.TypeCheck.Exceptions
{
    class VarAlreadyDeclaredException: TypingException
    {
        public override bool isWarning => false;
        public VarAlreadyDeclaredException(Token varName) : base($"var {varName.Lexeme} is already declared", varName.Line)
        {
        }
    }
}
