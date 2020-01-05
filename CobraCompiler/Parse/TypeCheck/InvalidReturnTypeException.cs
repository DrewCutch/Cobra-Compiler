using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.TypeCheck
{
    class InvalidReturnTypeException: TypingException
    {
        public InvalidReturnTypeException(Token returnStatement, CobraType returnedType, CobraType expectedType) : base($"Cannot return value of type {returnedType.Identifier} from function with {expectedType.Identifier} return type.", returnStatement.Line)
        {
            
        }
    }
}
