using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Parse.TypeCheck.Types
{
    class UnionInstanceType: CobraGenericInstance
    {
        public UnionInstanceType(string identifier, IEnumerable<CobraType> typeParams) : base(identifier, typeParams, UnionLangCobraGeneric.UnionGeneric)
        {

        }

        public override bool CanImplicitCast(CobraType other)
        {
            if(other is UnionInstanceType otherUnion)
            {
                HashSet<CobraType> myTypes = new HashSet<CobraType>(TypeParams);
                HashSet<CobraType> otherTypes = new HashSet<CobraType>(otherUnion.TypeParams);

                return myTypes.IsSupersetOf(otherTypes);
            }

            return base.CanImplicitCast(other) || TypeParams.Contains(other);
        }

        public override CobraType GetCommonParent(CobraType other)
        {
            if (Equals(other))
                return this;

            HashSet<CobraType> myTypes = new HashSet<CobraType>(TypeParams);

            if (other is UnionInstanceType otherUnion)
            {
                HashSet<CobraType> otherTypes = new HashSet<CobraType>(otherUnion.TypeParams);
                myTypes.UnionWith(otherTypes);

                return UnionLangCobraGeneric.UnionGeneric.CreateGenericInstance(new List<CobraType>(myTypes));
            }

            if (myTypes.Contains(other))
                return this;

            return base.GetCommonParent(other);
        }
    }
}
