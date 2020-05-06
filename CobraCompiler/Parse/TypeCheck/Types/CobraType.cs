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

        public IReadOnlyCollection<CobraType> Parents => _parents;

        private readonly HashSet<CobraType> _parents;

        public CobraType(string identifier): base(identifier)
        {
            _symbols = new Dictionary<string, CobraType>();
            _parents = new HashSet<CobraType>();
        }

        public CobraType(string identifier, params CobraType[] parents) : base(identifier)
        {
            _symbols = new Dictionary<string, CobraType>();
            _parents = new HashSet<CobraType>(parents);
        }

        protected void AddParent(CobraType parent)
        {
            foreach (KeyValuePair<string, CobraType> symbol in parent.Symbols)
                DefineSymbol(symbol.Key, symbol.Value);

            _parents.Add(parent);
        }

        public virtual bool HasSymbol(string symbol)
        {
            return GetSymbol(symbol) != null;
        }

        public virtual CobraType GetSymbol(string symbol)
        {
            if(_symbols.ContainsKey(symbol))
                return _symbols[symbol];

            foreach (CobraType parent in _parents)
                if (parent.HasSymbol(symbol))
                    return parent.GetSymbol(symbol);

            return null;
        }

        public virtual void DefineSymbol(string symbol, CobraType type)
        {
            _symbols[symbol] = type;
        }

        public virtual bool CanCastTo(CobraType other)
        {
            return Equals(other) || GetCommonParent(other).Equals(other);
        }

        public virtual CobraType GetCommonParent(CobraType other, bool unionize=true)
        {
            if (Equals(other))
                return this;

            if (_parents.Contains(other))
                return other;

            if (_parents.Count == 1)
                return _parents.First().GetCommonParent(other);

            if(unionize)
                return UnionLangCobraGeneric.UnionGeneric.CreateGenericInstance(new List<CobraType>(new CobraType[] {this, other}));

            return DotNetCobraType.Object;
        }

        public static CobraType GetCommonParent(IEnumerable<CobraType> types, bool unionize=true)
        {
            CobraType commonType = types.First();

            foreach (CobraType type in types)
                commonType = commonType.GetCommonParent(type, unionize);

            return commonType;
        }
    }
}
