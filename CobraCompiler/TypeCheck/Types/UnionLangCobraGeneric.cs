using System.Collections.Generic;
using System.Linq;
using CobraCompiler.Assemble.LangTypeAssemblers;

namespace CobraCompiler.TypeCheck.Types
{
    class UnionLangCobraGeneric: LangCobraGeneric
    {
        public static UnionLangCobraGeneric UnionGeneric = new UnionLangCobraGeneric();

        private UnionLangCobraGeneric() : base("union", -1, UnionAssembler.Assemble)
        {

        }

        public override CobraType CreateGenericInstance(IReadOnlyList<CobraType> typeParams)
        {
            List<CobraType> normalizedTypes = ApplyAssociativeProperty(typeParams);

            string unionName = GenerateGenericInstanceName(normalizedTypes);

            return new UnionInstanceType(unionName, normalizedTypes);
        }

        private List<CobraType> ApplyAssociativeProperty(IEnumerable<CobraType> typeParams)
        {
            List<CobraType> types = new List<CobraType>();

            foreach (CobraType typeParam in typeParams)
            {
                if (typeParam is UnionInstanceType union)
                {
                    types.AddRange(ApplyAssociativeProperty(union.OrderedTypeArguments));
                }
                else
                {
                    types.Add(typeParam);
                }
            }

            return new HashSet<CobraType>(types).ToList();
        }
    }
}
