using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.TypeCheck
{
    class IncorrectArgumentCountException: TypingException
    {
        public override bool isWarning => false;

        public IncorrectArgumentCountException(Token token, int expectedArgs, int providedArgs) : base($"Function expects {expectedArgs} arguments but is provided {providedArgs}", token.Line)
        {

        }
    }
}
