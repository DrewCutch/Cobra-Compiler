﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Assemble.LangTypeAssemblers;

namespace CobraCompiler.Parse.TypeCheck.Types
{
    class UnionLangCobraGeneric: LangCobraGeneric
    {
        public static UnionLangCobraGeneric UnionGeneric = new UnionLangCobraGeneric();

        private UnionLangCobraGeneric() : base("union", -1, UnionAssembler.Assemble)
        {

        }

        public override CobraGenericInstance CreateGenericInstance(IReadOnlyList<CobraType> typeParams)
        {
            List<CobraType> normalizedTypes = ApplyAssociativeProperty(typeParams);

            string unionName = GenerateGenericInstanceName(normalizedTypes);

            return new UnionInstanceType(unionName, normalizedTypes);
        }

        private List<CobraType> ApplyAssociativeProperty(IReadOnlyList<CobraType> typeParams)
        {
            List<CobraType> types = new List<CobraType>();

            foreach (CobraType typeParam in typeParams)
            {
                if (typeParam is UnionInstanceType union)
                {
                    types.AddRange(ApplyAssociativeProperty(union.TypeParams));
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