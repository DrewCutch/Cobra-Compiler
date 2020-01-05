using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler
{
    readonly struct CompilationOptions
    {
        public readonly string FilePath;
        public readonly CompilerFlags Flags;

        public CompilationOptions(string filePath, CompilerFlags flags)
        {
            FilePath = filePath;
            Flags = flags;
        }
    }
}
