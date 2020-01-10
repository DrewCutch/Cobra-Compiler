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
            if (HasFixedParamCount && typeParams.Count > NumberOfParams)
                throw new ArgumentException("Invalid number of parameters");

            return new CobraGenericInstance($"{Identifier}[{string.Join(",", typeParams.Select(param => param.Identifier))}]", typeParams, this);
        }
    }
}
