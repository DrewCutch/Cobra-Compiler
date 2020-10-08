using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class WriteToReadOnlySymbolException : TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }

        public override bool isWarning => false;

        public WriteToReadOnlySymbolException(AssignExpression assignExpression) : base($"Cannot assign to read only value")
        {
            FirstToken = assignExpression.FirstToken;
            LastToken = assignExpression.LastToken;
        }
    }
}
