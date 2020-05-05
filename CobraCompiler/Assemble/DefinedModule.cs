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
        public readonly IReadOnlyList<IAssemble> ToAssemble;
        public readonly TypeBuilder TypeBuilder;

        public DefinedModule(IEnumerable<IAssemble> toAssemble, TypeBuilder typeBuilder)
        {
            ToAssemble = new List<IAssemble>(toAssemble);
            TypeBuilder = typeBuilder;
        }
    }
}
