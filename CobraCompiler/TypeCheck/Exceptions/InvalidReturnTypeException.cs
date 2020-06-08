using CobraCompiler.Scanning;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class InvalidReturnTypeException: TypingException
    {
        public override bool isWarning => false;
        public InvalidReturnTypeException(Token returnStatement, CobraType returnedType, CobraType expectedType) : base($"Cannot return value of type {returnedType.Identifier} from function with {expectedType.Identifier} return type.", returnStatement.Line)
        {
            
        }
    }
}
