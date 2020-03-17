using System;
using System.Collections.Generic;
using System.Linq;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.TypeCheck.Operators;

namespace CobraCompiler.Parse.TypeCheck.Types
{
    abstract class CobraType: CobraTypeBase
    {
        protected CobraType(string identifier): base(identifier)
        {
        }

        public void DefineOperator(BinaryOperator op, FuncScope implementation)
        {
            if(implementation.Params.Count != 2 || implementation.Params[0].Item2 != op.Lhs || implementation.Params[1].Item2 != op.Rhs)
                throw new ArgumentException("Implementation does not handler operator");
        }

        public void DefineMethod(FuncScope implementation)
        {
            if(implementation.Params[0].Item2 != this)
                throw new ArgumentException($"First param of implementation must be of type {this.Identifier}");
        }

        public virtual bool CanImplicitCast(CobraType other)
        {
            return this.Equals(other);
        }

        public virtual CobraType GetCommonParent(CobraType other)
        {
            if (Equals(other))
                return this;

            return DotNetCobraType.Object;
        }

        public static CobraType GetCommonParent(IEnumerable<CobraType> types)
        {
            CobraType commonType = types.First();

            foreach (CobraType type in types)
                commonType = commonType.GetCommonParent(type);

            return commonType;
        }
    }
}
