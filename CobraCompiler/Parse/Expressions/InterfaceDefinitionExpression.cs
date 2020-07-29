using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse.Expressions
{
    class InterfaceDefinitionExpression: TypeInitExpression
    {
        private static int _counter = 0;

        public readonly IReadOnlyList<PropertyDefinitionExpression> Properties;
        public override Token FirstToken { get; }

        public InterfaceDefinitionExpression(Token bracket, IEnumerable<PropertyDefinitionExpression> properties, Token closingBracket) : base(GenIdentifier(bracket), new TypeInitExpression[]{}, closingBracket)
        {
            FirstToken = bracket;
            Properties = new List<PropertyDefinitionExpression>(properties);
        }

        private static Token[] GenIdentifier(Token bracket)
        {
            return new [] { bracket.InsertBefore(TokenType.Identifier, $"@Interface_{_counter++}", null) };
        }
    }
}
