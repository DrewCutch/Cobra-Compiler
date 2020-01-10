using CobraCompiler.Parse.TypeCheck.Types;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.TypeCheck.Operators
{
    class UnaryOperator: IOperator
    {
        public Operation Operation { get; }
        public CobraType ResultType { get; }

        public readonly CobraType OperandType;

        public UnaryOperator(Operation operation, CobraType operandType, CobraType resultType)
        {
            Operation = operation;
            ResultType = resultType;
            OperandType = operandType;
        }
    }
}
