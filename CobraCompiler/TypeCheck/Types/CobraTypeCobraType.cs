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

        public CobraTypeCobraType(CobraType cobraType) : base(cobraType.Identifier + "Type")
        {
            CobraType = cobraType;
        }
    }
}
