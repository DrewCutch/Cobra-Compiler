using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;

namespace CobraCompiler.Parse.Statements
{
    class IfStatement: Statement
    {
        public readonly Expression Condition;
        public readonly Statement Then;
        public readonly Statement Else;

        public IfStatement(Expression condition, Statement then, Statement @else)
        {
            Condition = condition;
            Then = then;
            Else = @else;
        }

    }
}
