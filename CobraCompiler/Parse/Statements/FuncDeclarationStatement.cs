using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Statements
{
    class FuncDeclarationStatement: Statement
    {
        public readonly Token Name;
        public readonly IReadOnlyList<ParamDeclarationStatement> Params;
        public readonly TypeInitExpression ReturnType;
        public readonly Statement Body;

        public FuncDeclarationStatement(Token name, IReadOnlyList<ParamDeclarationStatement> @params, TypeInitExpression returnType, Statement body)
        {
            Name = name;
            Params = @params;
            ReturnType = returnType;
            Body = body;
        }
    }
}
