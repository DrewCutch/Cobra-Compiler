using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Parse.TypeCheck;

namespace CobraCompiler.Parse.Scopes
{
    class ModuleScope: Scope
    {
        public readonly String Name;

        public ModuleScope(Scope parentScope, Statement body, String name) : base(parentScope, body)
        {
            Name = name;
        }
    }
}
