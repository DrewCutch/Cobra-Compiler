using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Statements;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.Parse.Scopes
{
    class ModuleScope: Scope
    {
        public readonly string Name;
        private readonly Dictionary<string, string> ImportAlias;

        public ModuleScope(Scope parentScope, Statement[] body, string name) : base(parentScope, body)
        {
            Name = name;
            ImportAlias = new Dictionary<string, string>();
        }

        public override void DefineType(string identifier, CobraType cobraType)
        {
            base.DefineType(identifier, cobraType);
            Parent.Declare($"{Name}.{identifier}", _types[identifier]);
        }

        public override void Declare(string var, CobraType type, bool overload = false)
        {
            base.Declare(var, type, overload);
            Parent.Declare($"{Name}.{var}", _vars[var]);
        }
    }
}
