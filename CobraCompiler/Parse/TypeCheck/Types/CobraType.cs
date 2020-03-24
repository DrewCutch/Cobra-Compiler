using System;
using System.Collections.Generic;
using System.Linq;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.TypeCheck.Operators;

namespace CobraCompiler.Parse.TypeCheck.Types
{
    class CobraType: CobraTypeBase
    {
        private readonly Dictionary<string, CobraType> _symbols;

        public IReadOnlyDictionary<string, CobraType> Symbols => _symbols;

        private readonly HashSet<CobraType> _parents;

        public CobraType(string identifier): base(identifier)
        {
            _symbols = new Dictionary<string, CobraType>();
            _parents = new HashSet<CobraType>();
        }

        public void DefineOperator(BinaryOperator op, FuncScope implementation)
        {
            if(implementation.Params.Count != 2 || implementation.Params[0].Item2 != op.Lhs || implementation.Params[1].Item2 != op.Rhs)
                throw new ArgumentException("Implementation does not handler operator");
        }

        public void DefineMethod(FuncScope implementation)
        {
            if(implementation.Params[0].Item2 != this)
                throw new ArgumentException($"First param of implementation must be of type {this.Identifier}");
        }

        public virtual bool HasSymbol(string symbol)
        {
            return _symbols.ContainsKey(symbol);
        }

        public virtual CobraType GetSymbol(string symbol)
        {
            return _symbols[symbol];
        }

        public virtual void DefineSymbol(string symbol, CobraType type)
        {
            _symbols[symbol] = type;
        }

        public virtual bool CanImplicitCast(CobraType other)
        {
            return this.Equals(other);
        }

        public virtual CobraType GetCommonParent(CobraType other)
        {
            if (Equals(other))
                return this;

            if (_parents.Contains(other))
                return other;

            if (_parents.Count == 1)
                return _parents.First().GetCommonParent(other);

            return UnionLangCobraGeneric.UnionGeneric.CreateGenericInstance(new List<CobraType>(new CobraType[] {this, other}));
        }

        public static CobraType GetCommonParent(IEnumerable<CobraType> types)
        {
            CobraType commonType = types.First();

            foreach (CobraType type in types)
                commonType = commonType.GetCommonParent(type);

            return commonType;
        }
    }
}
