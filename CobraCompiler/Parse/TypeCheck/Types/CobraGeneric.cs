using System;
using System.Collections.Generic;
using System.Linq;

namespace CobraCompiler.Parse.TypeCheck.Types
{
    class CobraGeneric: CobraTypeBase
    {
        public readonly int NumberOfParams;
        public bool HasFixedParamCount => NumberOfParams != -1;

        public CobraGeneric(string identifier, int numberOfParams): base(identifier)
        {
            NumberOfParams = numberOfParams;
        }

        public CobraGenericInstance CreatGenericInstance(params CobraType[] typeParams)
        {
            return CreateGenericInstance(typeParams);
        }

        public virtual CobraGenericInstance CreateGenericInstance(IReadOnlyList<CobraType> typeParams)
        {
            string instanceName = GenerateGenericInstanceName(typeParams);

            return new CobraGenericInstance(instanceName, typeParams, this);
        }

        protected string GenerateGenericInstanceName(IReadOnlyList<CobraType> typeParams)
        {
            if (HasFixedParamCount && typeParams.Count > NumberOfParams)
                throw new ArgumentException("Invalid number of parameters");

            return $"{Identifier}[{string.Join(",", typeParams.Select(param => param.Identifier))}]";
        }
    }
}
