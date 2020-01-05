using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Statements
{
    class ParamDeclarationStatement: Statement
    {
        public readonly Token Name;
        public readonly TypeInitExpression TypeInit;

        public ParamDeclarationStatement(Token name, TypeInitExpression typeInit)
        {
            Name = name;
            TypeInit = typeInit;
        }
    }
}
