using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class InvalidPanicTypeException: TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }
        public override bool isWarning => false;
        public InvalidPanicTypeException(PanicStatement panicStatement) : base($"Cannot panic with value of type {panicStatement.Argument.Type}, str is expected.")
        {
            FirstToken = panicStatement.Argument.FirstToken;
            LastToken = panicStatement.Argument.LastToken;
        }
    }
}
