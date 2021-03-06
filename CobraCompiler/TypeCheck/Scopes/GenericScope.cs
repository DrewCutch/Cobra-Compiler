﻿using System;
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
    class GenericScope: Scope

    {
        public GenericScope(Scope parentScope, Statement body) : base(parentScope, body)
        {

        }

        protected internal override Symbol Declare(Statement expr, string var, CobraType type, SymbolKind kind, Mutability mutability, bool overload = false, Symbol aliasOf = null)
        {
            return Parent.Declare(expr, var, type, kind, mutability, overload, aliasOf);
        }
    }
}
