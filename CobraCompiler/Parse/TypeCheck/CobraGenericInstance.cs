using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Parse.TypeCheck
{
    class CobraGenericInstance: CobraType
    {
        public readonly IReadOnlyList<CobraType> TypeParams;

        public CobraGenericInstance(string identifier, IEnumerable<CobraType> typeParams) : base(identifier)
        {
            TypeParams = new List<CobraType>(typeParams);
        }

        public override bool Equals(Object other)
        {
            CobraGenericInstance otherInstance = other as CobraGenericInstance;

            if (otherInstance == null)
                return false;


            if (Identifier != otherInstance.Identifier)
                return false;

            return TypeParams.SequenceEqual(otherInstance.TypeParams);
        }

        public override int GetHashCode()
        {
            int hashCode = Identifier.GetHashCode();

            foreach (CobraType cobraType in TypeParams)
            {
                hashCode = hashCode * 31 + (cobraType == null ? 0 : cobraType.GetHashCode());
            }

            return hashCode;
        }
    }
}
