using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Statements
{
    class TypeDeclarationStatement: Statement
    {
        public readonly Token Name;
        public readonly IReadOnlyList<Token> TypeArguments;

        public readonly IReadOnlyList<TypeInitExpression> Parents;
        public readonly InterfaceDefinitionExpression Interface;

        public TypeDeclarationStatement(Token name, IReadOnlyList<Token> typeArguments, IEnumerable<TypeInitExpression> parents, InterfaceDefinitionExpression interfaceBody)
        {
            Name = name;
            TypeArguments = typeArguments;
            Parents = new List<TypeInitExpression>(parents);
            Interface = interfaceBody;
        }
    }
}
