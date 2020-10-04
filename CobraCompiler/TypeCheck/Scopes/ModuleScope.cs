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
            Parent.DefineType($"{Name}.{identifier}", _types[identifier]);
        }
        
        protected internal override void Declare(Statement statement, string var, CobraType type, Mutability mutability, bool overload = false)
        {
            base.Declare(statement, var, type, mutability, overload);
            Parent.Declare(statement,$"{Name}.{var}", _vars[var].Type, mutability);
        }
    }
}
