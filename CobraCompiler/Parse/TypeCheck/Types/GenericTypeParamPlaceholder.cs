using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Parse.TypeCheck.Types
{
    class GenericTypeParamPlaceholder: CobraType
    {
        public GenericTypeParamPlaceholder(string identifier) : base(identifier)
        {

        }

        public override bool Equals(object obj)
        {
            if (obj is GenericTypeParamPlaceholder other)
                return Identifier == other.Identifier;

            return false;
        }

        public override int GetHashCode()
        {
            return Identifier.GetHashCode();
        }
    }
}
