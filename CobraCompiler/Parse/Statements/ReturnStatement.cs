using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Statements
{
    class ReturnStatement: Statement
    {
        public readonly Token Keyword;
        public readonly Expression Value;

        public ReturnStatement(Token keyword, Expression value)
        {
            Keyword = keyword;
            Value = value;
        }

    }
}
