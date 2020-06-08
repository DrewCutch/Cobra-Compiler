using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Parse.TypeCheck;
using CobraCompiler.Parse.TypeCheck.Types;

namespace CobraCompiler.Parse.Scopes
{
    class ModuleScope: Scope
    {
        public readonly string Name;
        private readonly Dictionary<string, string> ImportAlias;


        public ModuleScope(Scope parentScope, Statement body, string name) : base(parentScope, body)
        {
            Name = name;
            ImportAlias = new Dictionary<string, string>();
        }

        public override void Declare(string var, CobraType type, bool overload = false)
        {
            base.Declare(var, type, overload);
            Parent.Declare($"{Name}.{var}", _vars[var]);
        }
    }
}
