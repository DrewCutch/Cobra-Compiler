using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Expressions
{
    class PropertyDefinitionExpression: Expression
    {
        public override Token FirstToken => Identifier;
        public override Token LastToken => Type.LastToken;

        public readonly Token Identifier;
        public readonly TypeInitExpression Type;

        public PropertyDefinitionExpression(Token identifier, TypeInitExpression type)
        {
            Identifier = identifier;
            Type = type;
        }

        public override T Accept<T>(IExpressionVisitor<T> expressionVisitor)
        {
            throw new NotImplementedException();
        }

        public override void Accept<T>(IExpressionTraverser<T> expressionTraverser, T arg)
        {
            throw new NotImplementedException();
        }

        public override T Accept<T, TU>(IExpressionVisitorWithContext<T, TU> expressionVisitor, TU arg)
        {
            throw new NotImplementedException();
        }
    }
}
