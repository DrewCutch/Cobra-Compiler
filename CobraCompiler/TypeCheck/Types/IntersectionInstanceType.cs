using System.Collections.Generic;
using System.Linq;

namespace CobraCompiler.TypeCheck.Types
{
    class IntersectionInstanceType: CobraType
    {
        public IntersectionInstanceType(string identifier, IReadOnlyList<CobraType> typeParams) : 
            base(identifier,new List<CobraType>(), false, new List<CobraType>(), typeParams, IntersectionLangCobraGeneric.IntersectGeneric, -1, null)
        {
            foreach (CobraType typeParam in typeParams)
                AddParent(typeParam);
        }

        public override bool CanCastTo(CobraType other)
        {
            if (other is IntersectionInstanceType otherIntersection)
            {
                HashSet<CobraType> myTypes = new HashSet<CobraType>(OrderedTypeArguments);
                HashSet<CobraType> otherTypes = new HashSet<CobraType>(otherIntersection.OrderedTypeArguments);

                return myTypes.IsSupersetOf(otherTypes);
            }

            return base.CanCastTo(other) || TypeArguments.ContainsKey(other);
        }

        public override CobraType GetCommonParent(CobraType other, bool unionize = true)
        {
            if (Equals(other))
                return this;

            HashSet<CobraType> myTypes = new HashSet<CobraType>(OrderedTypeArguments);

            if (other is IntersectionInstanceType otherIntersection)
            {
                HashSet<CobraType> otherTypes = new HashSet<CobraType>(otherIntersection.OrderedTypeArguments);
                myTypes.IntersectWith(otherTypes);

                return IntersectionLangCobraGeneric.IntersectGeneric.CreateGenericInstance(new List<CobraType>(myTypes));
            }

            if (myTypes.Contains(other))
                return other;

            return base.GetCommonParent(other, unionize);
        }
    }
}
