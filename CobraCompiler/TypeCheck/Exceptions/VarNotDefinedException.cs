namespace CobraCompiler.TypeCheck.Exceptions
{
    class VarNotDefinedException: TypingException
    {
        public override bool isWarning => false;
        public VarNotDefinedException(string varName, int lineNumber) : base($"{varName} is not defined", lineNumber)
        {

        }
    }
}
