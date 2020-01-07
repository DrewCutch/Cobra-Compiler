using CobraCompiler.Parse.PrettyPrint;

namespace CobraCompiler.Parse.Expressions
{
    class GroupingExpression: Expression
    {
        public readonly Expression Inner;

        public GroupingExpression(Expression inner)
        {
            Inner = inner;
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
