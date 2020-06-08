using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.TypeCheck.Operators
{
    struct BinaryOperator: IOperator
    {
        public Operation Operation { get; }
        public CobraType ResultType { get; }

        public readonly CobraType Lhs;
        public readonly CobraType Rhs;

        public BinaryOperator(Operation operation, CobraType lhs, CobraType rhs, CobraType resultType)
        {
            Operation = operation;
            Lhs = lhs;
            Rhs = rhs;
            ResultType = resultType;
        }

        public CobraType GetFuncType()
        {
            return DotNetCobraGeneric.FuncType.CreateGenericInstance(Lhs, Rhs, ResultType);
        }
    }
}
