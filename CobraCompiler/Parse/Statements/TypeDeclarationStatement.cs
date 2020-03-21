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
        public readonly TypeInitExpression Type;

        public TypeDeclarationStatement(Token name, TypeInitExpression type)
        {
            Name = name;
            Type = type;
        }
    }
}
