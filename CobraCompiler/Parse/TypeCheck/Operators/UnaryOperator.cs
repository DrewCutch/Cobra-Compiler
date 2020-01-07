using CobraCompiler.Parse.TypeCheck.Types;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.TypeCheck.Operators
{
    class UnaryOperator: Operator
    {
        public readonly CobraType OperandType;

        public UnaryOperator(TokenType operatorToken, CobraType operandType, CobraType resultType) : base(operatorToken, resultType)
        {
            OperandType = operandType;
        }
    }
}
