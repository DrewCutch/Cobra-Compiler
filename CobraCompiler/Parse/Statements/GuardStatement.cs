using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Statements
{
    class GuardStatement: Statement
    {
        public Expression Condition { get; }
        public Statement Else { get; }

        public Token GuardKeyword { get; }
        public Token ElseKeyword { get; }

        public GuardStatement(Token guardKeyword, Expression condition, Token elseKeyword, Statement @else)
        {
            Condition = condition;
            Else = @else;

            GuardKeyword = guardKeyword;
            ElseKeyword = elseKeyword;
        }
    }
}
