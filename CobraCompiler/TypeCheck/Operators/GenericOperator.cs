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
        public readonly CobraType Rhs;
        public readonly CobraType Lhs;
        public readonly Func<CobraType, CobraType, CobraType> ReturnTypeResolver;

        public GenericOperator(Operation operation, CobraType lhs, CobraType rhs,
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
            CobraType lhs = Lhs.IsGenericType ? Lhs.CreateGenericInstance(CobraType.GenericPlaceholder("LHS", 0)) : Lhs;
            CobraType rhs = Rhs.IsGenericType ? Rhs.CreateGenericInstance(CobraType.GenericPlaceholder("RHS", 1)) : Rhs;
            return new BinaryOperator(Operation, lhs, rhs, ReturnTypeResolver(lhs, rhs));
        }

        public CobraType GetGenericFuncType()
        {
            CobraType lhs = Lhs.IsGenericType ? Lhs.CreateGenericInstance(CobraType.GenericPlaceholder("LHS", 0)) : Lhs;
            CobraType rhs = Rhs.IsGenericType ? Rhs.CreateGenericInstance(CobraType.GenericPlaceholder("RHS", 1)) : Rhs;
            return DotNetCobraGeneric.FuncType.CreateGenericInstance(lhs, rhs, ReturnTypeResolver(lhs, rhs));
        }
    }
}