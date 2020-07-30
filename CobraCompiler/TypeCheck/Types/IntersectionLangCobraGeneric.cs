using System.Collections.Generic;
using CobraCompiler.Assemble.LangTypeAssemblers;

namespace CobraCompiler.TypeCheck.Types
{
    class IntersectionLangCobraGeneric: LangCobraGeneric
    {
        public static IntersectionLangCobraGeneric IntersectGeneric = new IntersectionLangCobraGeneric();

        private IntersectionLangCobraGeneric() : base("intersect", -1, IntersectionAssembler.Assemble)
        {

        }

        public override CobraGenericInstance CreateGenericInstance(IReadOnlyList<CobraType> typeParams)
        {
            List<CobraType> normalizedTypes = ApplyAssociativeProperty(typeParams);

            string name = GenerateGenericInstanceName(normalizedTypes);

            return new IntersectionInstanceType(name, normalizedTypes);
        }

        private List<CobraType> ApplyAssociativeProperty(IReadOnlyList<CobraType> typeParams)
        {
            List<CobraType> types = new List<CobraType>();

            foreach (CobraType typeParam in typeParams)
            {
                if (typeParam is UnionInstanceType union)
                {
                    types.AddRange(ApplyAssociativeProperty(union.OrderedTypeParams));
                }
                else
                {
                    types.Add(typeParam);
                }
            }

            return types;
        }
    }
}
