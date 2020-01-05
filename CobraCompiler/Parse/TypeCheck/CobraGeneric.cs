using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Parse.TypeCheck
{
    class CobraGeneric
    {
        public static CobraGeneric[] BuiltInCobraGenerics =
        {
            new CobraGeneric("func", -1)
        };

        public readonly string Identifier;
        public readonly int NumberOfParams;
        public bool HasFixedParamCount => NumberOfParams != -1;

        public CobraGeneric(string identifier, int numberOfParams)
        {
            Identifier = identifier;
            NumberOfParams = numberOfParams;
        }

        public CobraGenericInstance CreateGenericInstance(IReadOnlyList<CobraType> typeParams)
        {
            if (HasFixedParamCount && typeParams.Count > NumberOfParams)
                throw new ArgumentException("Invalid number of parameters");

            return new CobraGenericInstance(Identifier, typeParams);
        }
    }
}
