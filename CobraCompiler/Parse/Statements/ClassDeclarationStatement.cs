using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Statements
{
    class ClassDeclarationStatement: Statement
    {
        public readonly Token Name;
        public readonly IReadOnlyList<Token> TypeArguments;

        public readonly TypeInitExpression Type;
        public readonly BlockStatement Body;

        public ClassDeclarationStatement(Token name, IReadOnlyList<Token> typeArguments, TypeInitExpression type, BlockStatement body)
        {
            Name = name;
            TypeArguments = typeArguments;
            Type = type;
            Body = body;
        }
    }
}
