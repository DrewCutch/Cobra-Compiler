using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Parse.Statements
{
    class BlockStatement: Statement
    {
        public readonly IReadOnlyList<Statement> Body;

        public BlockStatement(IEnumerable<Statement> statements)
        {
            Body = new List<Statement>(statements);
        }
    }
}
