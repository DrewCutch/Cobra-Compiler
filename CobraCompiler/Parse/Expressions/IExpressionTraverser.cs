namespace CobraCompiler.Parse.Expressions
{
    interface IExpressionTraverser<T>
    {
        void Visit(AssignExpression expr, T arg);
        void Visit(BinaryExpression expr, T arg);
        void Visit(CallExpression expr, T arg);
        void Visit(IndexExpression expr, T arg);
        void Visit(ListLiteralExpression expr, T arg);
        void Visit(LiteralExpression expr, T arg);
        void Visit(NullableAccessExpression expr, T arg);
        void Visit(TypeAssertionExpression expr, T arg);
        void Visit(TypeInitExpression expr, T arg);
        void Visit(UnaryExpression expr, T arg);
        void Visit(GetExpression expr, T arg);
        void Visit(GroupingExpression expr, T arg);
        void Visit(VarExpression expr, T arg);
    }
}
