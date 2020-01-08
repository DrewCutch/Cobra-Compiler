using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Parse.Expressions
{
    class IndexExpression: Expression
    {
        public readonly Expression Collection;
        public readonly IReadOnlyList<Expression> Indicies;

        public IndexExpression(Expression collection, IEnumerable<Expression> indicies)
        {
            Collection = collection;
            Indicies = new List<Expression>(indicies);
        }

        public override T Accept<T>(IExpressionVisitor<T> expressionVisitor)
        {
            return expressionVisitor.Visit(this);
        }

        public override void Accept<T>(IExpressionTraverser<T> expressionTraverser, T arg)
        {
            expressionTraverser.Visit(this, arg);
        }

        public override T Accept<T, TU>(IExpressionVisitorWithContext<T, TU> expressionVisitor, TU arg)
        {
            return expressionVisitor.Visit(this, arg);
        }
    }
}
