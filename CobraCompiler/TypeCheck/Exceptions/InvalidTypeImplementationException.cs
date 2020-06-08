using System.Collections.Generic;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class InvalidTypeImplementationException: TypingException
    {
        public override bool isWarning => false;

        public InvalidTypeImplementationException(string className, string typeName, IEnumerable<string> missing, int lineNumber) : 
            base($"{className} does not implement the following members of {typeName}: {string.Join(",", missing)}", lineNumber)
        {

        }
    }
}
