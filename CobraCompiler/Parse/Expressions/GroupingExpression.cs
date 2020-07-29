using CobraCompiler.Parse.PrettyPrint;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Expressions
{
    class GroupingExpression: Expression
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }

        public readonly Expression Inner;

        public GroupingExpression(Token openParen, Expression inner, Token closeParen)
        {
            FirstToken = openParen;
            Inner = inner;
            LastToken = closeParen;
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
