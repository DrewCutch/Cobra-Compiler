using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler
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
