using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Expressions
{
    class TypeAssertionExpression: Expression
    {
        public override Token FirstToken => Left.FirstToken;
        public override Token LastToken => Right.LastToken;

        public readonly Expression Left;
        public readonly Expression Right;
        public readonly bool NotType;

        public TypeAssertionExpression(Expression left, Expression right, bool notType)
        {
            Left = left;
            Right = right;
            NotType = notType;
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
