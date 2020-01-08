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
        T Visit(TypeInitExpression expr);
        T Visit(UnaryExpression expr);
        T Visit(GetExpression expr);
        T Visit(GroupingExpression expr);
        T Visit(VarExpression expr);
    }
}
/*  interface Visitor<R> {
    R visitAssignExpr(Assign expr);
    R visitBinaryExpr(Binary expr);
    R visitCallExpr(Call expr);
    R visitGetExpr(Get expr);
    R visitGroupingExpr(Grouping expr);
    R visitLiteralExpr(Literal expr);
    R visitLogicalExpr(Logical expr);
    R visitSetExpr(Set expr);
    R visitSuperExpr(Super expr);
    R visitThisExpr(This expr);
    R visitUnaryExpr(Unary expr);
    R visitVariableExpr(Variable expr);
  }
*/
