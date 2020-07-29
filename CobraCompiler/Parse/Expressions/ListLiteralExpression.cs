using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Expressions
{
    class ListLiteralExpression: Expression
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }

        public readonly IReadOnlyList<Expression> Elements;

        public ListLiteralExpression(Token openingBrace, IEnumerable<Expression> elements, Token closingBrace)
        {
            FirstToken = openingBrace;
            Elements = new List<Expression>(elements);
            LastToken = closingBrace;
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
