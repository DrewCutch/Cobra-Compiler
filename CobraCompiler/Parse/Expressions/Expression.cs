using System;
using CobraCompiler.Parse.PrettyPrint;
using CobraCompiler.Scanning;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.Parse.Expressions
{
    abstract class Expression
    {
        public abstract Token FirstToken { get; }
        public abstract Token LastToken { get; }

        private CobraType _type;
        public CobraType Type
        {
            get
            {
                if(_type == null)
                    throw new InvalidOperationException("The type has not been set for this expression");

                return _type;
            }

            set
            {
                if (value == null)
                    throw new InvalidOperationException("The type cannot be set to null");

                _type = value;
            }
        }

        protected Expression()
        {
            _type = null;
        }

        public abstract T Accept<T>(IExpressionVisitor<T> expressionVisitor);
        public abstract void Accept<T>(IExpressionTraverser<T> expressionTraverser, T arg);
        public abstract T Accept<T, TU>(IExpressionVisitorWithContext<T, TU> expressionVisitor, TU arg);
    }
}
