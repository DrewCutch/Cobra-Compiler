using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Statements
{
    class VarDeclarationStatement: Statement
    {
        public readonly Token Name;
        public readonly TypeInitExpression TypeInit;
        public readonly AssignExpression Assignment;

        public VarDeclarationStatement(Token name, TypeInitExpression typeInit, AssignExpression assignment)
        {
            Name = name;
            TypeInit = typeInit;
            Assignment = assignment;
        }
    }
}
