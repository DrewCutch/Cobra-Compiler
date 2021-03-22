using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class MissingGuardElseReturnException: TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }
        public override bool isWarning => false;

        public MissingGuardElseReturnException(GuardStatement guardStatement) : base("Else clause of guard statement must return.")
        {
            FirstToken = guardStatement.ElseKeyword;
            LastToken = guardStatement.ElseKeyword;
        }
    }
}
