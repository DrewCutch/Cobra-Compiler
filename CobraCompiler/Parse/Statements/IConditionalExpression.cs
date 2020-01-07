using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;

namespace CobraCompiler.Parse.Statements
{
    interface IConditionalExpression
    {
        Statement Then { get; }
        Statement Else { get; }
        Expression Condition { get; }
    }
}
