﻿using CobraCompiler.Parse.PrettyPrint;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Expressions
{
    class UnaryExpression: Expression
    {
        public override Token FirstToken => Op;
        public override Token LastToken => Right.LastToken;

        public readonly Token Op;
        public readonly Expression Right;

        public UnaryExpression(Token op, Expression right)
        {
            Op = op;
            Right = right;
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
