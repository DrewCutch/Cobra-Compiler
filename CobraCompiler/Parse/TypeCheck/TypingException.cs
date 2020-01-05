using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.ErrorLogging;

namespace CobraCompiler.Parse.TypeCheck
{
    abstract class TypingException : CompilingException
    {
        public override int LineNumber { get; }
        protected TypingException(string message, int lineNumber): base(message)
        {
            LineNumber = lineNumber;
        }
    }
}
