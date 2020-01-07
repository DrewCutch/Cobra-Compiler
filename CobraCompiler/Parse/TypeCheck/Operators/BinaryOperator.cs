using CobraCompiler.Parse.TypeCheck.Types;

namespace CobraCompiler.Parse.TypeCheck.Operators
{
    internal abstract class BinaryOperator: Operator
    {
        public readonly CobraType Lhs;
        public readonly CobraType Rhs;

        protected BinaryOperator(TokenType operatorToken, CobraType lhs, CobraType rhs, CobraType resultType) : base(operatorToken, resultType)
        {
            Lhs = lhs;
            Rhs = rhs;
        }
    }
}
