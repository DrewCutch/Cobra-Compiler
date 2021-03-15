using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;

namespace CobraCompiler.Parse.Statements
{
    class GuardStatement: Statement
    {
        public Expression Condition { get; }
        public Statement Else { get; }

        public GuardStatement(Expression condition, Statement @else)
        {
            Condition = condition;
            Else = @else;
        }
    }
}
