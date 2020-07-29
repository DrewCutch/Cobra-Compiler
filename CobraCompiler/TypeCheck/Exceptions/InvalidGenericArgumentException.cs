using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class InvalidGenericArgumentException: TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }
        public override bool isWarning => false;

        public InvalidGenericArgumentException(Expression argument) : base($"Generic type argument of type {argument.Type} must be a compile time type identifier")
        {
            FirstToken = argument.FirstToken;
            LastToken = argument.LastToken;
        }
    }
}
