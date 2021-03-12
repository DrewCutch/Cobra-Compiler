using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Expressions
{
    class NullableAccessExpression: MemberAccessExpression
    {
        public override Token FirstToken => Obj.FirstToken;
        public override Token LastToken => Name;

        public override Expression Obj { get; }
        public override Token Name { get; }

        public NullableAccessExpression(Expression obj, Token name)
        {
            Obj = obj;
            Name = name;
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
