using System;
using System.Collections.Generic;
using System.Linq;
using CobraCompiler.TypeCheck.Symbols;
using CobraCompiler.Util;

namespace CobraCompiler.TypeCheck.Types
{
    class CobraType
    {
        public readonly String Identifier;
        public virtual IReadOnlyCollection<CobraType> Parents => _parents;

        protected readonly HashSet<CobraType> _parents;

        protected readonly Dictionary<string, Symbol> _symbols;
        public virtual IReadOnlyDictionary<string, Symbol> Symbols => _symbols;

        // Generic properties
        private readonly List<CobraType> _typeParams;
        public virtual IReadOnlyList<CobraType> TypeParams => _typeParams;
        public virtual int NumberOfParams => _typeParams.Count;
        public virtual bool HasFixedParamCount => _typeParams.Count != 0;
        public bool IsGenericType { get; }

        // Generic instance properties
        public virtual IReadOnlyList<CobraType> OrderedTypeArguments { get; }
        public virtual Dictionary<CobraType, CobraType> TypeArguments { get; }
        public virtual CobraType GenericBase { get; }
        public bool HasPlaceholders => OrderedTypeArguments.Any(param => param.IsTypeParamPlaceholder || (param.IsConstructedGeneric && param.HasPlaceholders));
        public virtual bool IsConstructedGeneric => TypeArguments.Count > 0;

        // Type param properties
        public virtual bool IsTypeParamPlaceholder => TypeParamPlaceholderIndex != -1;
        public virtual int TypeParamPlaceholderIndex { get; }

        // Nullable type properties
        public virtual bool IsNullable => false;

        private readonly List<List<CobraType>> _callSigs;

        public virtual IEnumerable<IReadOnlyList<CobraType>> CallSigs
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

        // Protected constructor for any kind of cobra type
        protected CobraType(string identifier, IEnumerable<CobraType> parents, bool isGeneric, IEnumerable<CobraType> typeParams, IReadOnlyList<CobraType> typeArguments,
            CobraType genericBase, int typeParamIndex)
        {
            Identifier = identifier;
            _symbols = new Dictionary<string, Symbol>();
            _parents = new HashSet<CobraType>(parents);
            _callSigs = new List<List<CobraType>>();

            IsGenericType = isGeneric;

            _typeParams = new List<CobraType>(typeParams);
            if(!_typeParams.All(typeParam => typeParam.IsTypeParamPlaceholder))
                throw new ArgumentException("All typeParams must be TypeParamPlaceholders", nameof(typeParams));

            TypeArguments = genericBase?.CreateTypeParamMap(typeArguments) ?? new Dictionary<CobraType, CobraType>();
            OrderedTypeArguments = new List<CobraType>(typeArguments);

            GenericBase = genericBase;
            if(genericBase != null && !genericBase.IsGenericType)
                throw new ArgumentException("Generic base must be a generic type", nameof(genericBase));

            TypeParamPlaceholderIndex = typeParamIndex;
        }

        public static CobraType BasicCobraType(string identifier, params CobraType[] parents)
        {
            return new CobraType(identifier, parents, false, new List<CobraType>(), new List<CobraType>(), null, -1);
        }

        public static CobraType GenericCobraType(string identifier, IEnumerable<CobraType> typeParams)
        {
            return new CobraType(identifier, new List<CobraType>(), true, typeParams, new List<CobraType>(), null, -1);
        }

        public static CobraType GenericPlaceholder(string identifier, int index)
        {
            return new CobraType(identifier, new List<CobraType>(), false, new List<CobraType>(), new List<CobraType>(), null, index);
        }

        protected static CobraType GenericInstance(string identifier, IReadOnlyList<CobraType> typeArgs, CobraType genericBase)
        {
            return new CobraType(identifier, new List<CobraType>(), false, new List<CobraType>(), typeArgs, genericBase, -1);
        }

        public virtual void AddParent(CobraType parent)
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

        public virtual Symbol GetSymbol(string symbol)
        {
            if (IsConstructedGeneric)
                return GetGenericSymbol(symbol);

            return GetBasicSymbol(symbol);
        }

        private Symbol GetBasicSymbol(string symbol)
        {
            if (_symbols.ContainsKey(symbol))
                return _symbols[symbol];

            foreach (CobraType parent in _parents)
                if (parent.HasSymbol(symbol))
                    return parent.GetSymbol(symbol);

            return null;
        }

        private Symbol GetGenericSymbol(string symbol)
        {
            Symbol baseSymbol = GenericBase.GetSymbol(symbol);

            if (baseSymbol == null)
                return GetBasicSymbol(symbol);

            if (baseSymbol.Type.IsTypeParamPlaceholder)
                return new Symbol(baseSymbol.Declaration, TypeArguments[baseSymbol.Type], baseSymbol.Kind, baseSymbol.Mutability, baseSymbol.Lexeme);

            if (baseSymbol.Type.IsConstructedGeneric)
                return new Symbol(baseSymbol.Declaration, baseSymbol.Type.ReplacePlaceholders(OrderedTypeArguments), baseSymbol.Kind, baseSymbol.Mutability, baseSymbol.Lexeme);

            if (baseSymbol.Type.IsGenericType)
                return new Symbol(baseSymbol.Declaration, baseSymbol.Type.CreateGenericInstance(TypeParams), baseSymbol.Kind, baseSymbol.Mutability, baseSymbol.Lexeme);

            return baseSymbol;
        }

        public virtual void DefineSymbol(string symbolName, Symbol symbol, bool overload = false)
        {
            if (DeclaresSymbol(symbolName) && overload)
                _symbols[symbolName] = new Symbol(symbol.Declaration, IntersectionLangCobraGeneric.IntersectGeneric.CreateGenericInstance(_symbols[symbolName].Type, symbol.Type), symbol.Kind, Mutability.CompileTimeConstant, symbolName);
            else
                _symbols[symbolName] = symbol;
        }

        public virtual bool IsCallable()
        {
            if (_callSigs.Count > 0)
                return true;

            return _parents.Any(parent => parent.IsCallable());
        }

        public virtual bool IsCallable(params CobraType[] parameters) => IsCallable(parameters.ToList());

        public virtual bool IsCallable(List<CobraType> parameters)
        {
            if (!IsCallable())
                return false;

            if (CallReturn(parameters) != null)
                return true;

            return _parents.Any(parent => parent.CallReturn(parameters) != null);
        }

        public virtual CobraType CallReturn(params CobraType[] parameters) => CallReturn(parameters.ToList());

        public virtual CobraType CallReturn(List<CobraType> parameters)
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

        // Generic methods
        public CobraType CreateGenericInstance(Dictionary<CobraType, CobraType> typeArguments)
        {
            List<CobraType> orderedArgs = _typeParams.Select(param => typeArguments[param]).ToList();
            string instanceName = GenerateGenericInstanceName(orderedArgs);

            return GenericInstance(instanceName, orderedArgs, this);
        }

        public static List<CobraType> GenerateTypeParamPlaceholders(int numberOfParams)
        {
            List<CobraType> typeParams = new List<CobraType>();
            for (int n = 0; n < numberOfParams; n++)
                typeParams.Add( GenericPlaceholder("T" + n, n));

            return typeParams;
        }

        public Dictionary<CobraType, CobraType> CreateTypeParamMap(IEnumerable<CobraType> typeArgs)
        {
            Dictionary<CobraType, CobraType> typeArguments = new Dictionary<CobraType, CobraType>();

            foreach ((CobraType typeArg, int i)  in typeArgs.WithIndex())
            {
                if (HasFixedParamCount)
                    typeArguments[_typeParams[i]] = typeArg;
                else
                    typeArguments[GenericPlaceholder("T" + i, i)] = typeArg;
            }

            return typeArguments;
        }

        public CobraType CreateGenericInstance(params CobraType[] typeParams)
        {
            return CreateGenericInstance(new List<CobraType>(typeParams));
        }

        public virtual CobraType CreateGenericInstance(IReadOnlyList<CobraType> typeParams)
        {
            Dictionary<CobraType, CobraType> typeArguments = CreateTypeParamMap(typeParams);

            return CreateGenericInstance(typeArguments);
        }

        protected string GenerateGenericInstanceName(IReadOnlyList<CobraType> typeParams)
        {
            if (HasFixedParamCount && typeParams.Count > NumberOfParams)
                throw new ArgumentException("Invalid number of parameters");

            return $"{Identifier}[{string.Join(",", typeParams.Select(param => param.Identifier))}]";
        }

        public CobraType ReplacePlaceholders(IReadOnlyList<CobraType> typeArguments)
        {
            List<CobraType> typeParams = new List<CobraType>();

            foreach (CobraType typeParam in OrderedTypeArguments)
            {
                if (typeParam.IsTypeParamPlaceholder)
                    typeParams.Add(typeArguments[typeParam.TypeParamPlaceholderIndex]);
                else if (typeParam.IsConstructedGeneric)
                    typeParams.Add(typeParam.ReplacePlaceholders(typeArguments));
                else
                    typeParams.Add(typeParam);
            }

            return GenericBase.CreateGenericInstance(typeParams);
        }

        public override bool Equals(Object other)
        {
            CobraType otherType = other as CobraType;

            if (otherType == null)
                return false;


            if (Identifier != otherType.Identifier)
                return false;

            return TypeParamPlaceholderIndex == otherType.TypeParamPlaceholderIndex && IsNullable == otherType.IsNullable && TypeArguments.SequenceEqual(otherType.TypeArguments) && TypeParams.SequenceEqual(otherType.TypeParams);
        }

        public override int GetHashCode()
        {
            int hashCode = Identifier.GetHashCode();

            foreach (CobraType cobraType in OrderedTypeArguments)
            {
                hashCode = hashCode * 31 + (cobraType == null ? 0 : cobraType.GetHashCode());
            }

            foreach (CobraType cobraType in TypeParams)
            {
                hashCode = hashCode * 31 + (cobraType == null ? 0 : cobraType.GetHashCode());
            }

            hashCode = hashCode * 31 + TypeParamPlaceholderIndex;

            if (IsNullable)
                hashCode = ~hashCode;

            return hashCode;
        }
    }
}
