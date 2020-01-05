using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;

namespace CobraCompiler.Parse.PrettyPrint
{
    interface IExpressionTraverser<T>
    {
        void Visit(AssignExpression expr, T arg);
        void Visit(BinaryExpression expr, T arg);
        void Visit(CallExpression expr, T arg);
        void Visit(LiteralExpression expr, T arg);
        void Visit(TypeInitExpression expr, T arg);
        void Visit(UnaryExpression expr, T arg);
        void Visit(GetExpression expr, T arg);
        void Visit(GroupingExpression expr, T arg);
        void Visit(VarExpression expr, T arg);
    }
}
