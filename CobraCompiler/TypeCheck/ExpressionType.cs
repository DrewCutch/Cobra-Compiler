using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.TypeCheck.Symbols;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.TypeCheck
{
    class ExpressionType
    {
        public readonly CobraType Type;
        public readonly Mutability Mutability;
        public readonly Symbol Symbol;

        public ExpressionType(Symbol symbol)
        {
            Type = symbol.Type;
            Mutability = symbol.Mutability;
            Symbol = symbol;
        }

        public ExpressionType(CobraType type, Mutability mutability, Symbol symbol)
        {
            Type = type;
            Mutability = mutability;
            Symbol = symbol;
        }
    }
}
