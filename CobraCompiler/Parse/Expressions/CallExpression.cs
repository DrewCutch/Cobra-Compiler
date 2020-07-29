using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.PrettyPrint;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Expressions
{
    class CallExpression: Expression
    {
        public override Token FirstToken => Callee.FirstToken;
        public override Token LastToken => ClosingParen;

        public readonly Expression Callee;
        public readonly Token ClosingParen;
        public readonly IReadOnlyList<Expression> Arguments;

        public CallExpression(Expression callee, Token closingParen, IReadOnlyList<Expression> arguments)
        {
            Callee = callee;
            ClosingParen = closingParen;
            Arguments = arguments.ToList();
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
