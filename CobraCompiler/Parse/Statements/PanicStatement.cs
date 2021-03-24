using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Statements
{
    class PanicStatement: Statement
    {
        public Token Keyword;

        public Expression Argument;

        public PanicStatement(Token keyword, Expression argument)
        {
            Keyword = keyword;
            Argument = argument;
        }
    }
}
