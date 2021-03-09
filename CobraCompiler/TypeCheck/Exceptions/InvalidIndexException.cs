using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class InvalidIndexException: TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }
        public override bool isWarning => false;

        public InvalidIndexException(IndexExpression expr) : 
            base($"{expr.Collection.Type.Identifier} does not implement an index with params of type: ({string.Join(", ", expr.Indicies.Select(x => x.Type.Identifier))}).")
        {
            FirstToken = expr.FirstToken;
            LastToken = expr.LastToken;
        }
    }
}
