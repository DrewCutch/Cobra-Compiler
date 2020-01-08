using CobraCompiler.Parse.TypeCheck.Types;

namespace CobraCompiler.Parse.TypeCheck.Operators
{
    struct BinaryOperator: IOperator
    {
        public TokenType OperatorToken { get; }
        public CobraType ResultType { get; }

        public readonly CobraType Lhs;
        public readonly CobraType Rhs;

        public BinaryOperator(TokenType operatorToken, CobraType lhs, CobraType rhs, CobraType resultType)
        {
            OperatorToken = operatorToken;
            Lhs = lhs;
            Rhs = rhs;
            ResultType = resultType;
        }
    }
}
