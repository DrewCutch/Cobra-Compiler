using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Parse.TypeCheck.Exceptions
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
