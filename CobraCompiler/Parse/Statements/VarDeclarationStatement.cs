using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Statements
{
    class VarDeclarationStatement: Statement
    {
        public readonly Token KeyWord;
        public readonly Token Name;
        public readonly TypeInitExpression TypeInit;
        public readonly AssignExpression Assignment;
        public readonly bool IsVal;
        public VarDeclarationStatement(Token keyWord, Token name, TypeInitExpression typeInit, AssignExpression assignment)
        {
            KeyWord = keyWord;
            Name = name;
            TypeInit = typeInit;
            Assignment = assignment;

            IsVal = keyWord.Lexeme == "val";
        }
    }
}
