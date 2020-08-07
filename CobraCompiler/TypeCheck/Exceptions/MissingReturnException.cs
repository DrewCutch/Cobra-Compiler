using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Scanning;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class MissingReturnException : TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }

        public override bool isWarning => false;
        public MissingReturnException(Token scopeEnd, CobraType expectedType) : base($"Missing return from function with {expectedType} return type.")
        {
            FirstToken = scopeEnd;
            LastToken = scopeEnd;
        }
    }
}
