using System.Reflection.Emit;

namespace CobraCompiler.Parse.TypeCheck.Operators
{
    class DotNetBinaryOperator: BinaryOperator, IDotNetOperator
    {
        public static DotNetBinaryOperator[] BuiltinDotNetBinaryOperators = new[]
        {
            new DotNetBinaryOperator(TokenType.Plus, DotNetCobraType.Int, DotNetCobraType.Int, DotNetCobraType.Int,
                OpCodes.Add),
            new DotNetBinaryOperator(TokenType.Minus, DotNetCobraType.Int, DotNetCobraType.Int, DotNetCobraType.Int,
                OpCodes.Sub),
            new DotNetBinaryOperator(TokenType.Star, DotNetCobraType.Int, DotNetCobraType.Int, DotNetCobraType.Int,
                OpCodes.Mul),
            new DotNetBinaryOperator(TokenType.Slash, DotNetCobraType.Int, DotNetCobraType.Int, DotNetCobraType.Int,
                OpCodes.Div),
        };

        public OpCode OpCode { get; }

        public DotNetBinaryOperator(TokenType operatorToken, CobraType lhs, CobraType rhs, CobraType resultType, OpCode opCode) : base(operatorToken, lhs, rhs, resultType)
        {
            OpCode = opCode;
        }
    }
}
