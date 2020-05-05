using CobraCompiler.Parse.TypeCheck.Types;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.TypeCheck.Exceptions
{
    class OperatorNotDefinedException: TypingException
    {
        public override bool isWarning => false;
        public OperatorNotDefinedException(Token op, CobraType lhs, CobraType rhs) : 
            base($"Operator {op.Lexeme} not defined for {lhs.Identifier} and {rhs.Identifier}", op.Line)
        {
        }

        public OperatorNotDefinedException(Token op, CobraType operand) :
            base($"Operator {op.Lexeme} not defined for {operand.Identifier}", op.Line)
        {
        }
    }
}
