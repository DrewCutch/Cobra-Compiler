using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Statements
{
    class InitDeclarationStatement: FuncDeclarationStatement
    {
        public InitDeclarationStatement(Token keyWord, IReadOnlyList<ParamDeclarationStatement> @params, Statement body): base(keyWord, new List<Token>(), @params, null, body)
        { }
    }
}
