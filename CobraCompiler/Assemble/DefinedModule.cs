using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Assemble
{
    class DefinedModule
    {
        public readonly IReadOnlyList<FuncAssembler> FuncAssemblers;
        public readonly TypeBuilder TypeBuilder;

        public DefinedModule(IEnumerable<FuncAssembler> funcAssemblers, TypeBuilder typeBuilder)
        {
            FuncAssemblers = new List<FuncAssembler>(funcAssemblers);
            TypeBuilder = typeBuilder;
        }
    }
}
