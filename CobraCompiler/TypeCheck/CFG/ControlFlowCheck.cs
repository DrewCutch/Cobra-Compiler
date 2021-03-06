﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.CFG;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.TypeCheck.Symbols;
using CobraCompiler.Util;

namespace CobraCompiler.TypeCheck.CFG
{
    class ControlFlowCheck
    {
        public static Func<CFGNode, bool> IsAssigned(Symbol symbol)
        {
            return (node) =>
            {
                bool isInit = node.Scope is FuncScope funcScope && funcScope.FuncDeclaration.Name.Lexeme == "init";
                if (symbol.Mutability != Mutability.Mutable && node.Scope.Parent is ClassScope containingClass && !isInit && symbol.Kind == SymbolKind.ThisMember)
                    return true;

                if (node.IsRoot && node.Scope.Parent.IsDefined(symbol.Lexeme) && !isInit)
                    return true;

                return node.Assignments.ContainsKey(symbol);
            };
        }

        public static Func<CFGNode, bool> ReturnsUnderScope(Scope scope)
        {
            return (node) => node.Scope.IsContainedBy(scope) && (node.Next.OnlyOrDefault()?.IsTerminal ?? false);
        }
    }
}
