using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    
    class InvalidConditionTypeException: TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }

        public override bool isWarning => false;

        public InvalidConditionTypeException(Expression condition) : base($"Conditional statement condition must evaluate to a boolean")
        {
            FirstToken = condition.FirstToken;
            LastToken = condition.LastToken;
        }

    }
}
