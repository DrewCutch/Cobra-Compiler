using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class OperatorNotDefinedException: TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }

        public override bool isWarning => false;
        public OperatorNotDefinedException(BinaryExpression binary) : 
            base($"Operator {binary.Op.Lexeme} not defined for {binary.Left.Type} and {binary.Right.Type}")
        {
            FirstToken = binary.FirstToken;
            LastToken = binary.LastToken;
        }

        public OperatorNotDefinedException(UnaryExpression unary) :
            base($"Operator {unary.Op.Lexeme} not defined for {unary.Right.Type}")
        {
            FirstToken = unary.FirstToken;
            LastToken = unary.LastToken;
        }
    }
}
