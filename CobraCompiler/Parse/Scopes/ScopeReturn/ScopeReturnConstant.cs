using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Parse.Scopes.ScopeReturn
{
    class ScopeReturnConstant: ScopeReturnExpression
    {
        public override bool Returns { get; }

        public ScopeReturnConstant(bool val)
        {
            Returns = val;
        }
    }
}
