using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.TypeCheck.Operators
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
