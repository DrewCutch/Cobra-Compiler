using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class TypeNotDefinedException: TypingException
    {
        public override bool isWarning => false;
        public TypeNotDefinedException(Token typeName) : base($"{typeName.Lexeme} is not defined", typeName.Line)
        {

        }
    }
}
