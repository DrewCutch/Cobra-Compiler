using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Parse.Scopes.ScopeReturn
{
    class ScopeReturnBindExpression: ScopeReturnExpression
    {
        public override bool Returns => _scope.Returns;

        private Scope _scope;

        public ScopeReturnBindExpression(Scope scope)
        {
            _scope = scope;
        }
    }
}
