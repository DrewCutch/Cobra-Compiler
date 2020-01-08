using CobraCompiler.Parse.TypeCheck.Types;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.TypeCheck.Operators
{
    class UnaryOperator: IOperator
    {
        public TokenType OperatorToken { get; }
        public CobraType ResultType { get; }

        public readonly CobraType OperandType;

        public UnaryOperator(TokenType operatorToken, CobraType operandType, CobraType resultType)
        {
            OperatorToken = operatorToken;
            ResultType = resultType;
            OperandType = operandType;
        }
    }
}
