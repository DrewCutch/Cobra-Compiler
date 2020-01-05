using CobraCompiler.Parse.PrettyPrint;

namespace CobraCompiler.Parse.Expressions
{
    abstract class Expression
    {
        public abstract T Accept<T>(IExpressionVisitor<T> expressionVisitor);
        public abstract void Accept<T>(IExpressionTraverser<T> expressionTraverser, T arg);
    }
}
