using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.TypeCheck
{
    class InvalidArgumentException: TypingException
    {
        public override bool isWarning => false;

        public InvalidArgumentException(Token token) : base("Invalid argument", token.Line)
        {
        }
    }
}
