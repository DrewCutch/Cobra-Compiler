using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;

namespace CobraCompiler.Parse.Statements
{
    class WhileStatement: Statement
    {
        public readonly Expression Condition;
        public readonly Statement Body;
        public readonly Statement Else;

        public WhileStatement(Expression condition, Statement body, Statement @else)
        {
            Condition = condition;
            Body = body;
            Else = @else;
        }
    }
}
