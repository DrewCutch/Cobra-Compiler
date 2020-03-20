using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Assemble.LangTypeAssemblers
{
    abstract class LangTypeAssembler
    {
        public abstract Type Assemble(ModuleBuilder mb, params Type[] typeParams);
    }
}
