namespace CobraCompiler.Parse.TypeCheck
{
    class VarNotDefinedException: TypingException
    {
        public VarNotDefinedException(string varName, int lineNumber) : base($"{varName} is not defined", lineNumber)
        {

        }
    }
}
