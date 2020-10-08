using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.TypeCheck.Symbols
{
    class MutabilityUtils
    {
        public static Mutability GetResultMutability(params Mutability[] mutability)
        {
            if (mutability.All(m => m == Mutability.CompileTimeConstant || m == Mutability.CompileTimeConstantResult))
                return Mutability.CompileTimeConstantResult;


            return Mutability.Result;
        }
    }
}
