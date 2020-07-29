using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class InvalidMemberException : TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }
        public override bool isWarning => false;
        public InvalidMemberException(GetExpression getExpression) : base($"{getExpression.Obj.Type} does not have member {getExpression.Name.Lexeme}")
        {
            FirstToken = getExpression.Obj.FirstToken;
            LastToken = getExpression.Name;
        }
    }
}
