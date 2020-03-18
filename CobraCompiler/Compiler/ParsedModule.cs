using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Scanning;

namespace CobraCompiler.Compiler
{
    class ParsedModule: ScannedModule
    {
        public readonly IReadOnlyList<Statement> Statements;

        public ParsedModule(ScannedModule module, IEnumerable<Statement> statements) : base(module, module.Tokens)
        {
            Statements = new List<Statement>(statements);
        }
    }
}
