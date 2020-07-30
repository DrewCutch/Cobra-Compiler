﻿using System.Collections.Generic;

namespace CobraCompiler.TypeCheck.Types
{
    class UnionInstanceType: CobraGenericInstance
    {
        public UnionInstanceType(string identifier, IReadOnlyList<CobraType> typeParams) : base(identifier, typeParams, UnionLangCobraGeneric.UnionGeneric)
        {
            CobraType commonParent = GetCommonParent(typeParams, unionize:false);
            foreach (KeyValuePair<string, CobraType> symbol in commonParent.Symbols)
                DefineSymbol(symbol.Key, symbol.Value);

            AddParent(commonParent);
        }

        public override bool CanCastTo(CobraType other)
        {
            if (other is UnionInstanceType otherUnion)
            {
                HashSet<CobraType> myTypes = new HashSet<CobraType>(OrderedTypeParams);
                HashSet<CobraType> otherTypes = new HashSet<CobraType>(otherUnion.OrderedTypeParams);

                return myTypes.IsSubsetOf(otherTypes);
            }

            return base.CanCastTo(other);
        }

        public override CobraType GetCommonParent(CobraType other, bool unionize=true)
        {
            if (Equals(other))
                return this;

            HashSet<CobraType> myTypes = new HashSet<CobraType>(OrderedTypeParams);

            if (other is UnionInstanceType otherUnion)
            {
                HashSet<CobraType> otherTypes = new HashSet<CobraType>(otherUnion.OrderedTypeParams);
                myTypes.UnionWith(otherTypes);

                return UnionLangCobraGeneric.UnionGeneric.CreateGenericInstance(new List<CobraType>(myTypes));
            }

            if (myTypes.Contains(other))
                return this;

            return base.GetCommonParent(other, unionize);
        }
    }
}
