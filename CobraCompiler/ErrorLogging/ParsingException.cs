using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Scanning;

namespace CobraCompiler.ErrorLogging
{
    class ParsingException: CompilingException
    {
        public override int LineNumber { get; }
        public override bool isWarning => false;

        public ParsingException(Token token, String message) : base($"{message} at {token.Lexeme}")
        {
            LineNumber = token.Line;
        }
        
    }
}
