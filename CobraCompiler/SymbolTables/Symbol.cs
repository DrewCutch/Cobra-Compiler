using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.TypeCheck;
using CobraCompiler.Parse.TypeCheck.Types;

namespace CobraCompiler.SymbolTables
{
    class Symbol
    {
        public readonly SymbolFlag Flags;
        public readonly string Identifier;
        public readonly CobraType Type;
        public readonly SymbolTable SubTable;

        public Symbol(SymbolFlag flags, string identifier, CobraType type, SymbolTable subTable=null)
        {
            Flags = flags;
            Identifier = identifier;
            Type = type;
            SubTable = subTable;
        }
    }
}
