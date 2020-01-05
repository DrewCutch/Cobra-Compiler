using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Parse.Scopes
{
    interface IScopeVisitor<T>
    {
        T Visit(Scope scope);
        T Visit(FuncScope funcScope);
    }
}
