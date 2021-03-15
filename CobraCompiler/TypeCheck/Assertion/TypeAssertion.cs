using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.TypeCheck.Assertion
{
    class TypeAssertion
    {
        public TypeAssertionExpression Expression { get; }
        public Symbol Symbol { get; }
        public CobraType Type { get; }

        public TypeAssertion(TypeAssertionExpression expr, Symbol symbol, CobraType type)
        {
            Expression = expr;
            Symbol = symbol;
            Type = type;
        }

        public TypeAssertion Inverted()
        {
            if(Symbol.Type.IsNullable && Symbol.Type.NullableBase.Equals(Type))
                return new TypeAssertion(Expression, Symbol, DotNetCobraType.Null);
            else if(Symbol.Type.IsNullable && Type.Equals(DotNetCobraType.Null))
                return new TypeAssertion(Expression, Symbol, Symbol.Type.NullableBase);

            throw new NotImplementedException();
        }
    }
}
