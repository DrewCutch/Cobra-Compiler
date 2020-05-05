using CobraCompiler.Parse.TypeCheck.Types;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.TypeCheck.Exceptions
{
    class InvalidReturnTypeException: TypingException
    {
        public override bool isWarning => false;
        public InvalidReturnTypeException(Token returnStatement, CobraType returnedType, CobraType expectedType) : base($"Cannot return value of type {returnedType.Identifier} from function with {expectedType.Identifier} return type.", returnStatement.Line)
        {
            
        }
    }
}
