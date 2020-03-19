using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Parse.TypeCheck
{
    class InvalidMemberException : TypingException
    {
        public override bool isWarning => false;
        public InvalidMemberException(string typeName, string memberName, int lineNumber) : base($"{typeName} does not have member {memberName}", lineNumber)
        {

        }
    }
}
