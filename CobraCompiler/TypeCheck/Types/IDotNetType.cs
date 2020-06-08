using System;

namespace CobraCompiler.TypeCheck.Types
{
    interface IDotNetType
    {
        Type Type { get; }
    }
}
