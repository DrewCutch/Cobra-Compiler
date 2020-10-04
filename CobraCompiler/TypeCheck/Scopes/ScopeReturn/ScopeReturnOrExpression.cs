using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Parse.Scopes.ScopeReturn
{
    class ScopeReturnOrExpression: ScopeReturnExpression
    {
        public override bool Returns => _operands.Any(operand => operand.Returns);

        private readonly List<ScopeReturnExpression> _operands;

        public ScopeReturnOrExpression(params ScopeReturnExpression[] operands)
        {
            _operands = new List<ScopeReturnExpression>(operands);
        }

        public void AddOperand(ScopeReturnExpression operand)
        {
            _operands.Add(operand);
        }
    }
}
