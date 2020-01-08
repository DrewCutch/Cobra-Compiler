using System;
using System.Collections.Generic;

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

        public virtual CobraGenericInstance CreateGenericInstance(IReadOnlyList<CobraType> typeParams)
        {
            if (HasFixedParamCount && typeParams.Count > NumberOfParams)
                throw new ArgumentException("Invalid number of parameters");

            return new CobraGenericInstance(Identifier, typeParams, this);
        }
    }
}
