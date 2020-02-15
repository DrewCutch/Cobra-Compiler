namespace CobraCompiler.Parse.TypeCheck
{
    class VarNotDefinedException: TypingException
    {
        public override bool isWarning => false;
        public VarNotDefinedException(string varName, int lineNumber) : base($"{varName} is not defined", lineNumber)
        {

        }
    }
}
