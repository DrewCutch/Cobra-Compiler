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

        public InterfaceDefinitionExpression(Token bracket, IEnumerable<PropertyDefinitionExpression> properties) : base(GenIdentifier(bracket.Line), new TypeInitExpression[]{})
        {
            Properties = new List<PropertyDefinitionExpression>(properties);
        }

        private static Token[] GenIdentifier(int lineNumber)
        {
            return new [] { new Token(TokenType.Identifier, $"@Interface_{_counter++}", null, lineNumber)};
        }
    }
}
