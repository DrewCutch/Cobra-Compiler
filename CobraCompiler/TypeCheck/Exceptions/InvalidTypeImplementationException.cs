using System.Collections.Generic;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class InvalidTypeImplementationException: TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }

        public override bool isWarning => false;

        public InvalidTypeImplementationException(ClassDeclarationStatement implementation, TypeInitExpression type, IEnumerable<string> missing) : 
            base($"{implementation.Name.Lexeme} does not implement the following members of {type.IdentifierStr}: {string.Join(",", missing)}")
        {
            FirstToken = implementation.Name;
            LastToken = type.LastToken;
        }
    }
}
