using System;
using System.Collections.Generic;

namespace CobraCompiler.TypeCheck.Operators
{
    class Operator
    {
        private static readonly Dictionary<Operation, string> OverloadSpecialNames = new Dictionary<Operation, string>()
        {
            {Operation.Add, "op_Addition"},
            {Operation.CompareEqual, "op_Equality"},
            {Operation.CompareGreater, "op_GreaterThan"},
            {Operation.CompareGreaterEqual, "op_GreaterThanOrEqual"},
            {Operation.CompareLess, "op_LessThan"},
            {Operation.CompareLessEqual, "op_LessThanOrEqual"},
            {Operation.CompareNotEqual, "op_Inequality"},
            {Operation.Devide, "op_Division"},
            {Operation.Get, "get_Item" },
            {Operation.Multiply, "op_Multiply"},
            {Operation.Subtract, "op_Subtraction"}
        };

        public static string GetOverloadSpecialName(Operation operation)
        {
            return OverloadSpecialNames[operation];
        }

        public static Operation GetOperation(TokenType tokenType)
        {
            switch (tokenType)
            {
                case TokenType.LeftBracket:
                    return Operation.Get;
                case TokenType.Minus:
                    return Operation.Subtract;
                case TokenType.Plus:
                    return Operation.Add;
                case TokenType.Slash:
                    return Operation.Devide;
                case TokenType.Star:
                    return Operation.Multiply;
                case TokenType.BangEqual:
                    return Operation.CompareNotEqual;
                case TokenType.EqualEqual:
                    return Operation.CompareEqual;
                case TokenType.Greater:
                    return Operation.CompareGreater;
                case TokenType.GreaterEqual:
                    return Operation.CompareGreaterEqual;
                case TokenType.Less:
                    return Operation.CompareLess;
                case TokenType.LessEqual:
                    return Operation.CompareLessEqual;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, null);
            }
        }
    }
}
