using System.Collections.Generic;
using System.Linq;

namespace CobraCompiler.TypeCheck.Types
{
    class IntersectionInstanceType: CobraGenericInstance
    {
        public IntersectionInstanceType(string identifier, IReadOnlyList<CobraType> typeParams) : base(identifier, typeParams, IntersectionLangCobraGeneric.IntersectGeneric)
        {
            foreach (CobraType typeParam in typeParams)
                AddParent(typeParam);
        }

        public override bool CanCastTo(CobraType other)
        {
            if (other is IntersectionInstanceType otherIntersection)
            {
                HashSet<CobraType> myTypes = new HashSet<CobraType>(OrderedTypeParams);
                HashSet<CobraType> otherTypes = new HashSet<CobraType>(otherIntersection.OrderedTypeParams);

                return myTypes.IsSupersetOf(otherTypes);
            }

            return base.CanCastTo(other) || OrderedTypeParams.Contains(other);
        }

        public override CobraType GetCommonParent(CobraType other, bool unionize = true)
        {
            if (Equals(other))
                return this;

            HashSet<CobraType> myTypes = new HashSet<CobraType>(OrderedTypeParams);

            if (other is IntersectionInstanceType otherIntersection)
            {
                HashSet<CobraType> otherTypes = new HashSet<CobraType>(otherIntersection.OrderedTypeParams);
                myTypes.IntersectWith(otherTypes);

                return IntersectionLangCobraGeneric.IntersectGeneric.CreateGenericInstance(new List<CobraType>(myTypes));
            }

            if (myTypes.Contains(other))
                return other;

            return base.GetCommonParent(other, unionize);
        }
    }
}
