using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Parse.TypeCheck
{
    class InvalidAssignmentException: TypingException
    {
        public InvalidAssignmentException(string expectedType, string rightType, int lineNumber) : base($"Cannot assign {rightType} to var of type {expectedType}", lineNumber)
        {
        }
    }
}
