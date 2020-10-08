using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.TypeCheck.Symbols
{
    enum Mutability
    {
        Result,
        Mutable,
        AssignOnce,
        ReadOnly,
        CompileTimeConstant,
        CompileTimeConstantResult
    }
}
