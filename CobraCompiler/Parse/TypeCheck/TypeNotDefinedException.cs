using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.TypeCheck
{
    class TypeNotDefinedException: TypingException
    {
        public override bool isWarning => false;
        public TypeNotDefinedException(Token typeName) : base($"{typeName.Lexeme} is not defined", typeName.Line)
        {

        }
    }
}
