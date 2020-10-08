using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Statements;
using CobraCompiler.TypeCheck.Symbols;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.Parse.Scopes
{
    class GenericScope: Scope

    {
        public GenericScope(Scope parentScope, Statement body) : base(parentScope, body)
        {

        }

        protected internal override void Declare(Statement expr, string var, CobraType type, Mutability mutability, bool overload = false)
        {
            Parent.Declare(expr, var, type, mutability, overload);
        }
    }
}
