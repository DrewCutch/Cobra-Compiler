using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Expressions
{
    class DeclareVarExpression
    {
        public readonly Token Name;
        public readonly Token Type;

        public DeclareVarExpression(Token name, Token type)
        {
            Name = name;
            Type = type;
        }
    }
}
