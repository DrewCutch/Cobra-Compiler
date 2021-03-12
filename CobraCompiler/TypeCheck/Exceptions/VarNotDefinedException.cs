using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class VarNotDefinedException: TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }

        public override bool isWarning => false;
        public VarNotDefinedException(VarExpression varExpression) : base($"{varExpression.Name.Lexeme} is not defined")
        {
            FirstToken = varExpression.Name;
            LastToken = varExpression.Name;
        }

        public VarNotDefinedException(MemberAccessExpression expr, string resolvedName) : base($"{resolvedName} is not defined")
        {
            FirstToken = expr.FirstToken;
            LastToken = expr.LastToken;
        }
    }
}
