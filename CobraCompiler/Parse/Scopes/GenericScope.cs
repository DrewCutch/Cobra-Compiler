using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Statements;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.Parse.Scopes
{
    class GenericScope: Scope

    {
        public GenericScope(Scope parentScope, Statement body) : base(parentScope, body)
        {

        }

        public override void Declare(string var, CobraType type, bool overload = false)
        {
            Parent.Declare(var, type, overload);
        }
    }
}
