using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Scanning;

namespace CobraCompiler.ErrorLogging
{
    class ScanningException: CompilingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }
        public override bool isWarning => false;

        public ScanningException(Token token) : base($"Invalid token: \"{token.Lexeme}\"")
        {
            FirstToken = token;
            LastToken = token;
        }
    }
}
