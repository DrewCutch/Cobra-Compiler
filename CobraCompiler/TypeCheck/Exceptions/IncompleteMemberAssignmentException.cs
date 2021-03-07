using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class IncompleteMemberAssignmentException: TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }
        public override bool isWarning => false;

        public IncompleteMemberAssignmentException(Symbol symbol, FuncDeclarationStatement statement) : base($"val {symbol.Lexeme} is not assigned in init.")
        {
            FirstToken = statement.Name;
            LastToken = statement.Name;
        }

    }
}
