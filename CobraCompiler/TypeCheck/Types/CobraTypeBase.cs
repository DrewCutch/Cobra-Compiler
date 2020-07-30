using System;
using System.Collections.Generic;

namespace CobraCompiler.TypeCheck.Types
{
    abstract class CobraTypeBase
    {
        public readonly String Identifier;

        public IReadOnlyCollection<CobraType> Parents => _parents;

        protected readonly HashSet<CobraType> _parents;

        protected readonly Dictionary<string, CobraType> _symbols;
        public IReadOnlyDictionary<string, CobraType> Symbols => _symbols;

        protected CobraTypeBase(string identifier)
        {
            Identifier = identifier;
            _symbols = new Dictionary<string, CobraType>();
            _parents = new HashSet<CobraType>();
        }

        
        public virtual bool HasSymbol(string symbol)
        {
            return GetSymbol(symbol) != null;
        }

        private bool DeclaresSymbol(string symbol)
        {
            return _symbols.ContainsKey(symbol);
        }

        public virtual CobraType GetSymbol(string symbol)
        {
            if (_symbols.ContainsKey(symbol))
                return _symbols[symbol];

            foreach (CobraType parent in _parents)
                if (parent.HasSymbol(symbol))
                    return parent.GetSymbol(symbol);

            return null;
        }

        public virtual void DefineSymbol(string symbol, CobraType type, bool overload = false)
        {
            if (DeclaresSymbol(symbol) && overload)
                _symbols[symbol] =
                    IntersectionLangCobraGeneric.IntersectGeneric.CreateGenericInstance(_symbols[symbol], type);
            else
                _symbols[symbol] = type;
        }
    }
}
