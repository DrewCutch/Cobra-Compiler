using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class InvalidMemberException : TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }
        public override bool isWarning => false;
        public InvalidMemberException(MemberAccessExpression expr) : base($"{expr.Obj.Type} does not have member {expr.Name.Lexeme}")
        {
            FirstToken = expr.Obj.FirstToken;
            LastToken = expr.Name;
        }
    }
}
