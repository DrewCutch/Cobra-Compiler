using System;
using System.Collections.Generic;
using System.Linq;

namespace CobraCompiler.TypeCheck.Types
{
    class CobraType
    {
        public readonly String Identifier;
        public IReadOnlyCollection<CobraType> Parents => _parents;

        protected readonly HashSet<CobraType> _parents;

        protected readonly Dictionary<string, CobraType> _symbols;
        public IReadOnlyDictionary<string, CobraType> Symbols => _symbols;

        public IEnumerable<IReadOnlyList<CobraType>> CallSigs
        {
            get
            {
                foreach (List<CobraType> callSig in _callSigs)
                    yield return callSig;

                foreach (CobraType parent in _parents)
                    foreach (IReadOnlyList<CobraType> parentCallSig in parent.CallSigs)
                        yield return parentCallSig;

            }
        }

        private readonly List<List<CobraType>> _callSigs;

        public CobraType(string identifier)
        {
            Identifier = identifier;
            _symbols = new Dictionary<string, CobraType>();
            _parents = new HashSet<CobraType>();
            _callSigs = new List<List<CobraType>>();
        }

        public CobraType(string identifier, params CobraType[] parents)
        {
            Identifier = identifier;
            _symbols = new Dictionary<string, CobraType>();
            _parents = new HashSet<CobraType>(parents);
            _callSigs = new List<List<CobraType>>();
            _callSigs = new List<List<CobraType>>();
        }

        
        public void AddParent(CobraType parent)
        {
            _parents.Add(parent);
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

        public bool IsCallable()
        {
            if (_callSigs.Count > 0)
                return true;

            return _parents.Any(parent => parent.IsCallable());
        }

        public bool IsCallable(params CobraType[] parameters) => IsCallable(parameters.ToList());

        public bool IsCallable(List<CobraType> parameters)
        {
            if (!IsCallable())
                return false;

            if (CallReturn(parameters) != null)
                return true;

            return _parents.Any(parent => parent.CallReturn(parameters) != null);
        }

        public CobraType CallReturn(params CobraType[] parameters) => CallReturn(parameters.ToList());

        public CobraType CallReturn(List<CobraType> parameters)
        {
            if (!IsCallable())
                return null;

            foreach (List<CobraType> sig in _callSigs)
            {
                if (sig.Count != parameters.Count + 1)
                    continue;

                bool matches = true;

                for (int i = 0; i < sig.Count - 1; i++)
                    if (!parameters[i].CanCastTo(sig[i]))
                    {
                        matches = false;
                        break;
                    }

                if (matches)
                    return sig.Last();
            }

            foreach (CobraType parent in _parents)
            {
                CobraType callReturn = parent.CallReturn(parameters);
                if (callReturn != null)
                    return callReturn;
            }

            return null;
        }

        protected void AddCallSig(params CobraType[] sig) => AddCallSig(sig.ToList());

        protected void AddCallSig(List<CobraType> sig)
        {
            _callSigs.Add(sig);
        }

        public virtual bool CanCastTo(CobraType other)
        {
            return Equals(other) || GetCommonParent(other).Equals(other);
        }

        public virtual CobraType GetCommonParent(CobraType other, bool unionize = true)
        {
            if (Equals(other))
                return this;

            if (_parents.Contains(other))
                return other;

            if (_parents.Count == 1)
                return _parents.First().GetCommonParent(other);

            if (unionize)
                return UnionLangCobraGeneric.UnionGeneric.CreateGenericInstance(new List<CobraType>(new CobraType[] { this, other }));

            return DotNetCobraType.Object;
        }

        public static CobraType GetCommonParent(IEnumerable<CobraType> types, bool unionize=true)
        {
            CobraType commonType = types.First();

            foreach (CobraType type in types)
                commonType = commonType.GetCommonParent(type, unionize);

            return commonType;
        }

        public override string ToString()
        {
            return Identifier;
        }
    }
}
