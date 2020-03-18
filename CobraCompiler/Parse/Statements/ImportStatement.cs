using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Statements
{
    class ImportStatement: Statement
    {
        public readonly Token Keyword;

        public readonly Expression Import;

        public ImportStatement(Token keyword, Expression import)
        {
            Keyword = keyword;
            Import = import;
        }

    }
}
