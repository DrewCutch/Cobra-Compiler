using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.TypeCheck
{
    class VarAlreadyDeclaredException: TypingException
    {
        public override bool isWarning => false;
        public VarAlreadyDeclaredException(Token varName) : base($"var {varName.Lexeme} is already declared", varName.Line)
        {
        }
    }
}
