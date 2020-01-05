using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.ErrorLogging
{
    class ScanningException: CompilingException
    {
        public override int LineNumber { get; }

        public ScanningException(String lexeme, int lineNumber) : base($"Invalid token: \"{lexeme}\"")
        {
            LineNumber = lineNumber;
        }
    }
}
