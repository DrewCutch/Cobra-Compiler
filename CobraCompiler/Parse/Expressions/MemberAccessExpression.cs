using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Expressions
{
    abstract class MemberAccessExpression: Expression
    {
        public abstract Expression Obj { get; }
        public abstract Token Name { get; }
    }
}
