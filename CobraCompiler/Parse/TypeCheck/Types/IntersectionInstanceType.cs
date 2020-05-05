using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Parse.TypeCheck.Types
{
    class IntersectionInstanceType: CobraGenericInstance
    {
        public IntersectionInstanceType(string identifier, IEnumerable<CobraType> typeParams) : base(identifier, typeParams, IntersectionLangCobraGeneric.IntersectGeneric)
        {
            foreach (CobraType typeParam in typeParams)
                _parents.Add(typeParam);
        }

        public override bool CanImplicitCast(CobraType other)
        {
            if (other is IntersectionInstanceType otherIntersection)
            {
                HashSet<CobraType> myTypes = new HashSet<CobraType>(TypeParams);
                HashSet<CobraType> otherTypes = new HashSet<CobraType>(otherIntersection.TypeParams);

                return otherTypes.IsSupersetOf(myTypes);
            }

            return base.CanImplicitCast(other) || TypeParams.Contains(other);
        }

        public override CobraType GetCommonParent(CobraType other, bool unionize = true)
        {
            if (Equals(other))
                return this;

            HashSet<CobraType> myTypes = new HashSet<CobraType>(TypeParams);

            if (other is IntersectionInstanceType otherIntersection)
            {
                HashSet<CobraType> otherTypes = new HashSet<CobraType>(otherIntersection.TypeParams);
                myTypes.IntersectWith(otherTypes);

                return IntersectionLangCobraGeneric.IntersectGeneric.CreateGenericInstance(new List<CobraType>(myTypes));
            }

            if (myTypes.Contains(other))
                return other;

            return base.GetCommonParent(other, unionize);
        }
    }
}
