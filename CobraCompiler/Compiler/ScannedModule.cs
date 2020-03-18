using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Scanning;

namespace CobraCompiler.Compiler
{
    class ScannedModule: Module
    {
        public readonly IReadOnlyList<Token> Tokens;

        public ScannedModule(Module module, IEnumerable<Token> tokens) : 
            base(module.Name, module.SystemPath, module.File)
        {
            Tokens = new List<Token>(tokens);
        }
    }
}
