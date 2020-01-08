using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Parse.TypeCheck.Types
{
    abstract class CobraTypeBase
    {
        public readonly String Identifier;

        protected CobraTypeBase(string identifier)
        {
            Identifier = identifier;
        }
    }
}
