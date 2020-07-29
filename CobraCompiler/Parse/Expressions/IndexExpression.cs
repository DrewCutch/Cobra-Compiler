using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Expressions
{
    class IndexExpression: Expression
    {
        public override Token FirstToken => Collection.FirstToken;
        public override Token LastToken => ClosingBrace;

        public readonly Expression Collection;
        public readonly Token ClosingBrace;
        public readonly IReadOnlyList<Expression> Indicies;

        public IndexExpression(Token closingBrace, Expression collection, IEnumerable<Expression> indicies)
        {
            ClosingBrace = closingBrace;
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
