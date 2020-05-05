namespace CobraCompiler.Parse.TypeCheck.Exceptions
{
    class InvalidAssignmentException: TypingException
    {
        public override bool isWarning => false;
        public InvalidAssignmentException(string expectedType, string rightType, int lineNumber) : base($"Cannot assign {rightType} to var of type {expectedType}", lineNumber)
        {
        }
    }
}
