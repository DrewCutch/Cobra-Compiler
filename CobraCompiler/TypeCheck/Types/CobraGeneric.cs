using System;
using System.Collections.Generic;
using System.Linq;

namespace CobraCompiler.TypeCheck.Types
{
    class CobraGeneric: CobraType
    {
        private readonly List<GenericTypeParamPlaceholder> _typeParams;
        public IReadOnlyList<GenericTypeParamPlaceholder> TypeParams => _typeParams;

        public int NumberOfParams => _typeParams.Count;
        public bool HasFixedParamCount => _typeParams.Count != 0;

        public CobraGeneric(string identifier, IEnumerable<GenericTypeParamPlaceholder> typeParams): base(identifier)
        {
            _typeParams = new List<GenericTypeParamPlaceholder>(typeParams);
        }

        public CobraGenericInstance CreateGenericInstance(Dictionary<GenericTypeParamPlaceholder, CobraType> typeArguments)
        {
            List<CobraType> orderedArgs = _typeParams.Select(param => typeArguments[param]).ToList();
            string instanceName = GenerateGenericInstanceName(orderedArgs);

            return new CobraGenericInstance(instanceName, orderedArgs, this);
        }

        public static List<GenericTypeParamPlaceholder> GenerateTypeParamPlaceholders(int numberOfParams)
        {
            List<GenericTypeParamPlaceholder> typeParams = new List<GenericTypeParamPlaceholder>();
            for (int n = 0; n < numberOfParams; n++)
                typeParams.Add(new GenericTypeParamPlaceholder("T" + n, n));

            return typeParams;
        }

        public Dictionary<GenericTypeParamPlaceholder, CobraType> CreateTypeParamMap(IReadOnlyList<CobraType> typeParams)
        {
            Dictionary<GenericTypeParamPlaceholder, CobraType> typeArguments = new Dictionary<GenericTypeParamPlaceholder, CobraType>();

            
            for (int i = 0; i < typeParams.Count; i++)
            {
                if (HasFixedParamCount)
                    typeArguments[_typeParams[i]] = typeParams[i];
                else
                    typeArguments[new GenericTypeParamPlaceholder("T" + i, i)] = typeParams[i];
            }

            return typeArguments;
        }

        public CobraGenericInstance CreateGenericInstance(params CobraType[] typeParams)
        {
            return CreateGenericInstance(new List<CobraType>(typeParams));
        }

        public virtual CobraGenericInstance CreateGenericInstance(IReadOnlyList<CobraType> typeParams)
        {
            Dictionary<GenericTypeParamPlaceholder, CobraType> typeArguments = CreateTypeParamMap(typeParams);

            return CreateGenericInstance(typeArguments);
        }

        protected string GenerateGenericInstanceName(IReadOnlyList<CobraType> typeParams)
        {
            if (HasFixedParamCount && typeParams.Count > NumberOfParams)
                throw new ArgumentException("Invalid number of parameters");

            return $"{Identifier}[{string.Join(",", typeParams.Select(param => param.Identifier))}]";
        }
    }
}
