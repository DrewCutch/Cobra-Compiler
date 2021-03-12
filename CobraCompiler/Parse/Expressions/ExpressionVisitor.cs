namespace CobraCompiler.Parse.Expressions
{
    interface IExpressionVisitor<T>
    {
        T Visit(AssignExpression expr);
        T Visit(BinaryExpression expr);
        T Visit(CallExpression expr);
        T Visit(IndexExpression expr);
        T Visit(ListLiteralExpression expr);
        T Visit(LiteralExpression expr);
        T Visit(NullableAccessExpression expr);
        T Visit(TypeInitExpression expr);
        T Visit(UnaryExpression expr);
        T Visit(GetExpression expr);
        T Visit(GroupingExpression expr);
        T Visit(VarExpression expr);
    }
}
