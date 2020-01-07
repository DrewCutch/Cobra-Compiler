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
        public readonly IReadOnlyList<TypeInitExpression> GenericParams;
        public readonly string IdentifierStr;
        public readonly string IdentifierStrWithoutParams;
        public bool IsGenericInstance => GenericParams.Count > 0;

        public TypeInitExpression(IEnumerable<Token> identifier, IEnumerable<TypeInitExpression> genericParams)
        {
            Identifier = new List<Token>(identifier);
            GenericParams = new List<TypeInitExpression>(genericParams);
            IdentifierStr = string.Join(".", Identifier.Select(token => token.Lexeme).ToArray());
            IdentifierStrWithoutParams = IdentifierStr;

            if (GenericParams.Count > 0)
                IdentifierStr += $"[{string.Join(",", GenericParams.Select(typeInit => typeInit.IdentifierStr).ToArray())}]";
        }

        public override T Accept<T>(IExpressionVisitor<T> expressionVisitor)
        {
            return expressionVisitor.Visit(this);
        }

        public override void Accept<T>(IExpressionTraverser<T> expressionTraverser, T arg)
        {
            expressionTraverser.Visit(this, arg);
        }

        public override T Accept<T, TU>(IExpressionVisitorWithContext<T, TU> expressionVisitor, TU arg)
        {
            return expressionVisitor.Visit(this, arg);
        }
    }
}
