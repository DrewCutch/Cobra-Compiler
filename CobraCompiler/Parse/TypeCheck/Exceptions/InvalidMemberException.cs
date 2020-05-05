namespace CobraCompiler.Parse.TypeCheck.Exceptions
{
    class InvalidMemberException : TypingException
    {
        public override bool isWarning => false;
        public InvalidMemberException(string typeName, string memberName, int lineNumber) : base($"{typeName} does not have member {memberName}", lineNumber)
        {

        }
    }
}
