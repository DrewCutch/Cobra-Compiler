using System.Collections.Generic;
using System.Linq;

namespace CobraCompiler.Parse.TypeCheck.Types
{
    abstract class CobraType
    {
        public readonly string Identifier;

        protected CobraType(string identifier)
        {
            Identifier = identifier;
        }

        public virtual bool CanImplicitCast(CobraType other)
        {
            return this.Equals(other);
        }

        public virtual CobraType GetCommonParent(CobraType other)
        {
            if (Equals(other))
                return this;

            return DotNetCobraType.Object;
        }

        public static CobraType GetCommonParent(IEnumerable<CobraType> types)
        {
            CobraType commonType = types.First();

            foreach (CobraType type in types)
                commonType = commonType.GetCommonParent(type);

            return commonType;
        }
    }
}
