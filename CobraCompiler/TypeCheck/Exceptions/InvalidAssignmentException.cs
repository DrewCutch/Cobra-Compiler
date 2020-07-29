using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class InvalidAssignmentException: TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }

        public override bool isWarning => false;
        public InvalidAssignmentException(AssignExpression assignExpression) : base($"Cannot assign {assignExpression.Value.Type} to var of type {assignExpression.Target.Type}")
        {
            FirstToken = assignExpression.FirstToken;
            LastToken = assignExpression.LastToken;
        }
    }
}
