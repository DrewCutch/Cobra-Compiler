using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;

namespace CobraCompiler.Parse.Statements
{
    class WhileStatement: Statement, IConditionalExpression
    {
        public readonly Expression Condition;
        public Statement Then { get; }
        public Statement Else { get; }

        public WhileStatement(Expression condition, Statement then, Statement @else)
        {
            Condition = condition;
            Then = then;
            Else = @else;
        }
    }
}
