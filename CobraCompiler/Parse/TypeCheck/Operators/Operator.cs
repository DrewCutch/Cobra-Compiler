using CobraCompiler.Parse.TypeCheck.Types;

namespace CobraCompiler.Parse.TypeCheck.Operators
{
    internal abstract class Operator
    {
        public readonly TokenType OperatorToken;
        public readonly CobraType ResultType;

        protected Operator(TokenType operatorToken, CobraType resultType)
        {
            OperatorToken = operatorToken;
            ResultType = resultType;
        }
    }
}
