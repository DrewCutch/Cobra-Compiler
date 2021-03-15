using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Statements;
using CobraCompiler.TypeCheck;
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
        
        protected internal override Symbol Declare(Statement statement, string var, CobraType type, SymbolKind kind, Mutability mutability, bool overload = false, Symbol aliasOf = null)
        {
            Symbol baseSymbol = base.Declare(statement, var, type, kind, mutability, overload, aliasOf);
            
            Parent.Declare(statement,$"{Name}.{var}", _vars[var].Type, kind, mutability, overload, baseSymbol);

            return baseSymbol;
        }
    }
}
