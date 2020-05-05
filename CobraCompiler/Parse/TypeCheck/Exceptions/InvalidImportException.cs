namespace CobraCompiler.Parse.TypeCheck.Exceptions
{
    class InvalidImportException : TypingException
    {
        public override bool isWarning => false;
        public InvalidImportException(string type, int lineNumber) : base($"Cannot import {type}", lineNumber)
        {

        }
    }
}
