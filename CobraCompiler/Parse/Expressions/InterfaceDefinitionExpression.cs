using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Expressions
{
    class InterfaceDefinitionExpression: Expression
    {
        private static int _counter = 0;

        public readonly IReadOnlyList<PropertyDefinitionExpression> Properties;
        public override Token FirstToken { get; }
        public override Token LastToken { get; }
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

        public InterfaceDefinitionExpression(Token bracket, IEnumerable<PropertyDefinitionExpression> properties, Token closingBracket)
        {
            FirstToken = bracket;
            LastToken = closingBracket;
            Properties = new List<PropertyDefinitionExpression>(properties);
        }

        private static Token[] GenIdentifier(Token bracket)
        {
            return new [] { bracket.InsertBefore(TokenType.Identifier, $"@Interface_{_counter++}", null) };
        }
    }
}
