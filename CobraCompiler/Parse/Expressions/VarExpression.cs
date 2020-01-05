using CobraCompiler.Parse.PrettyPrint;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Expressions
{
    class VarExpression : Expression
    {
        public readonly Token Name;

        public VarExpression(Token name)
        {
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
    }
}
