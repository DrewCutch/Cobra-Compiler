using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class VarAlreadyDeclaredException: TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }

        public override bool isWarning => false;
        public VarAlreadyDeclaredException(VarDeclarationStatement varDeclaration) : base($"{varDeclaration.KeyWord.Lexeme} {varDeclaration.Name.Lexeme} is already declared")
        {
            FirstToken = varDeclaration.KeyWord;
            LastToken = varDeclaration.TypeInit?.LastToken ?? varDeclaration.Name;
        }

        public VarAlreadyDeclaredException(ParamDeclarationStatement paramDeclaration) : base($"param {paramDeclaration.Name.Lexeme} is already declared")
        {
            FirstToken = paramDeclaration.Name;
            LastToken = paramDeclaration.TypeInit.LastToken;
        }
    }
}
