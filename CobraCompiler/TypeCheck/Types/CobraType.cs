using System;
using System.Collections.Generic;
using System.Linq;

namespace CobraCompiler.TypeCheck.Types
{
    class CobraType: CobraTypeBase
    {
        

        public IReadOnlyList<IReadOnlyList<CobraType>> CallSigs => _callSigs;

        private readonly List<List<CobraType>> _callSigs;

        public CobraType(string identifier): base(identifier)
        {
            _callSigs = new List<List<CobraType>>();
        }

        public CobraType(string identifier, params CobraType[] parents) : base(identifier)
        {
            _callSigs = new List<List<CobraType>>();
        }

        public bool IsCallable()
        {
            return _callSigs.Count > 0;
        }

        public bool IsCallable(params CobraType[] parameters) => IsCallable(parameters.ToList());

        public bool IsCallable(List<CobraType> parameters)
        {
            if (!IsCallable())
                return false;

            return CallReturn(parameters) != null;
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

        protected void AddParent(CobraType parent)
        {
            foreach (KeyValuePair<string, CobraType> symbol in parent.Symbols)
                DefineSymbol(symbol.Key, symbol.Value);

            foreach (List<CobraType> callSig in parent._callSigs)
                AddCallSig(callSig);

            _parents.Add(parent);
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
