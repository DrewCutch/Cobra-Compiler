namespace CobraCompiler.Parse.TypeCheck.Exceptions
{
    class InvalidOperationException: TypingException
    {
        public override bool isWarning => false;
        public InvalidOperationException(int lineNumber) : base("Cannot perform operation", lineNumber)
        {

        }
    }
}
