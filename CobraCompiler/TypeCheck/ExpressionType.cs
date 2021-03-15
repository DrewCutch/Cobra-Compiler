using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.TypeCheck.Assertion;
using CobraCompiler.TypeCheck.Symbols;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.TypeCheck
{
    class ExpressionType
    {
        public readonly CobraType Type;
        public readonly Mutability Mutability;
        public readonly Symbol Symbol;

        public HashSet<TypeAssertion> TypeAssertions { get; }

        public ExpressionType(Symbol symbol): this(symbol.Type, symbol.Mutability, symbol, new List<TypeAssertion>())
        {
            
        }

        public ExpressionType(CobraType type, Mutability mutability, Symbol symbol) : this(type, mutability, symbol,
            new List<TypeAssertion>())
        {

        }

        public ExpressionType(CobraType type, Mutability mutability, Symbol symbol, IEnumerable<TypeAssertion> _typeAssertions)
        {
            Type = type;
            Mutability = mutability;
            Symbol = symbol;
            TypeAssertions = new HashSet<TypeAssertion>(_typeAssertions);
        }


    }
}
