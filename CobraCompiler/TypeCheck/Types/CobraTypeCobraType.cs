using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.TypeCheck.Types
{
    class CobraTypeCobraType: CobraType
    {
        public readonly CobraType CobraType;

        public CobraTypeCobraType(CobraType cobraType) : base(cobraType.Identifier + "Type", new List<CobraType>(),false, new List<CobraType>(), new List<CobraType>(), null, -1)
        {
            CobraType = cobraType;
        }
    }
}
