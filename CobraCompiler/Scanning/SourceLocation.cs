using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Scanning
{
    public readonly struct SourceLocation
    {
        public readonly string SourceFile;
        public readonly int Line;
        public readonly int CharIndex;

        public SourceLocation(string sourceFile, int line, int charIndex)
        {
            SourceFile = sourceFile;
            Line = line;
            CharIndex = charIndex;
        }

        public override string ToString()
        {
            return $"{SourceFile}:{Line}:{CharIndex}";
        }
    }
}
