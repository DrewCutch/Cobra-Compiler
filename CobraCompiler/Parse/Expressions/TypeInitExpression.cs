using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.PrettyPrint;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Expressions
{
    class TypeInitExpression: Expression
    {
        public readonly IReadOnlyList<Token> Identifier;
        public readonly String IdentifierStr;

        public TypeInitExpression(IEnumerable<Token> identifier)
        {
            Identifier = new List<Token>(identifier);
            IdentifierStr = String.Join(".", Identifier.Select(token => token.Lexeme).ToArray());
        }

        public override T Accept<T>(IExpressionVisitor<T> expressionVisitor)
        {
            return expressionVisitor.Visit(this);
        }

        public override void Accept<T>(IExpressionTraverser<T> expressionTraverser, T arg)
        {
            expressionTraverser.Visit(this, arg);
        }
    }
}
