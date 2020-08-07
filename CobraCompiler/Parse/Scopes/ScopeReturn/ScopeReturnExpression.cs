using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Parse.Scopes
{
    abstract class ScopeReturnExpression
    {
        public abstract bool Returns { get; }
    }
}
