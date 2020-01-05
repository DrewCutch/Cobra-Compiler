using CobraCompiler.Parse.Expressions;

namespace CobraCompiler.Parse.Statements
{
    class ExpressionStatement: Statement
    {
        public readonly Expression Expression;

        public ExpressionStatement(Expression expression)
        {
            Expression = expression;
        }
    }
}
