using System;

namespace CobraCompiler.Compiler
{
    [Flags]
    enum CompilerFlags
    {
        None = 0,
        Debug = PrintScan | PrintParse,
        PrintScan = (1 << 0),
        PrintParse = (1 << 1),
        HideErrors = (1 << 2),
        HideWarnings = (1 << 3)
    }
}
