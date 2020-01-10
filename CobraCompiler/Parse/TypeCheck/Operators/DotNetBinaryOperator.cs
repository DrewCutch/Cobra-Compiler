using System.Reflection.Emit;
using CobraCompiler.Parse.TypeCheck.Types;

namespace CobraCompiler.Parse.TypeCheck.Operators
{
    struct DotNetBinaryOperator: IOperator, IDotNetOperator
    {
        public static DotNetBinaryOperator[] OpCodeDotNetBinaryOperators = new[]
        {
            new DotNetBinaryOperator(Operation.Add, DotNetCobraType.Int, DotNetCobraType.Int, DotNetCobraType.Int,
                OpCodes.Add),
            new DotNetBinaryOperator(Operation.Subtract, DotNetCobraType.Int, DotNetCobraType.Int, DotNetCobraType.Int,
                OpCodes.Sub),
            new DotNetBinaryOperator(Operation.Multiply, DotNetCobraType.Int, DotNetCobraType.Int, DotNetCobraType.Int,
                OpCodes.Mul),
            new DotNetBinaryOperator(Operation.Devide, DotNetCobraType.Int, DotNetCobraType.Int, DotNetCobraType.Int,
                OpCodes.Div),
            new DotNetBinaryOperator(Operation.CompareGreater, DotNetCobraType.Int, DotNetCobraType.Int, DotNetCobraType.Bool,
                OpCodes.Cgt),
            new DotNetBinaryOperator(Operation.CompareLess, DotNetCobraType.Int, DotNetCobraType.Int, DotNetCobraType.Bool,
                OpCodes.Clt),
            new DotNetBinaryOperator(Operation.CompareEqual, DotNetCobraType.Int, DotNetCobraType.Int, DotNetCobraType.Bool,
                OpCodes.Ceq),
        };


        public OpCode OpCode { get; }
        public BinaryOperator Operator { get; }
        public Operation Operation => Operator.Operation;
        public CobraType ResultType => Operator.ResultType;

        public DotNetBinaryOperator(Operation operation, CobraType lhs, CobraType rhs, CobraType resultType, OpCode opCode)
        {
            OpCode = opCode;
            Operator = new BinaryOperator(operation, lhs, rhs, resultType);
        }
    }
}
