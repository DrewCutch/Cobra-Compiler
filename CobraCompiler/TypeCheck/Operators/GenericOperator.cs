using System;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.TypeCheck.Operators
{
    class GenericOperator
    {
        public static GenericOperator[] DotNetGenericOperators = new GenericOperator[]
        {
        };

        public readonly Operation Operation;
        public readonly CobraTypeBase Rhs;
        public readonly CobraTypeBase Lhs;
        public readonly Func<CobraType, CobraType, CobraType> ReturnTypeResolver;

        public GenericOperator(Operation operation, CobraTypeBase lhs, CobraTypeBase rhs,
            Func<CobraType, CobraType, CobraType> returnTypeResolver)
        {
            Operation = operation;
            Lhs = lhs;
            Rhs = rhs;
            ReturnTypeResolver = returnTypeResolver;
        }

        public BinaryOperator GetOperatorInstance(CobraType lhs, CobraType rhs)
        {
            return new BinaryOperator(Operation, lhs, rhs, ReturnTypeResolver(lhs, rhs));
        }

        public BinaryOperator GetGenericBinaryOperator()
        {
            CobraType lhs = Lhs is CobraGeneric gen1 ? gen1.CreateGenericInstance(new GenericTypeParamPlaceholder("LHS", 0)) : ((CobraType)Lhs);
            CobraType rhs = Rhs is CobraGeneric gen2 ? gen2.CreateGenericInstance(new GenericTypeParamPlaceholder("RHS", 1)) : ((CobraType)Rhs);
            return new BinaryOperator(Operation, lhs, rhs, ReturnTypeResolver(lhs, rhs));
        }

        public CobraType GetGenericFuncType()
        {
            CobraType lhs = Lhs is CobraGeneric gen1 ? gen1.CreateGenericInstance(new GenericTypeParamPlaceholder("LHS", 0)) : ((CobraType) Lhs);
            CobraType rhs = Rhs is CobraGeneric gen2 ? gen2.CreateGenericInstance(new GenericTypeParamPlaceholder("RHS", 1)) : ((CobraType) Rhs);
            return DotNetCobraGeneric.FuncType.CreateGenericInstance(lhs, rhs, ReturnTypeResolver(lhs, rhs));
        }
    }
}