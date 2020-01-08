using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Parse.Expressions
{
    interface IExpressionVisitorWithContext<out T, in TU>
    {
        T Visit(AssignExpression expr, TU arg);
        T Visit(BinaryExpression expr, TU arg);
        T Visit(CallExpression expr, TU arg);
        T Visit(ListLiteralExpression expr, TU arg);
        T Visit(LiteralExpression expr, TU arg);
        T Visit(TypeInitExpression expr, TU arg);
        T Visit(UnaryExpression expr, TU arg);
        T Visit(GetExpression expr, TU arg);
        T Visit(GroupingExpression expr, TU arg);
        T Visit(VarExpression expr, TU arg);
    }
}
