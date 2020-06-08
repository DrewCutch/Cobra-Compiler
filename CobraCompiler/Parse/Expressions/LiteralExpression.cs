using CobraCompiler.Parse.PrettyPrint;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.Parse.Expressions
{
    class LiteralExpression: Expression
    {
        public readonly object Value;
        public readonly CobraType LiteralType;

        public LiteralExpression(object value, CobraType literalType)
        {
            Value = value;
            LiteralType = literalType;
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
