using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Scanning
{
    struct Token
    {
        public readonly TokenType Type;
        public readonly string Lexeme;
        public readonly object Literal;
        public readonly int Line;

        public Token(TokenType type, string lexeme, object literal, int line)
        {
            Type = type;
            Lexeme = lexeme;
            Literal = literal;
            Line = line;
        }

        public override string ToString()
        {
            return $"{{{Type} {Lexeme} {Literal}}}";
        }
    }

}
