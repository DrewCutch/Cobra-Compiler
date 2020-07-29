using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Scanning
{
    public class Token
    {
        public readonly TokenType Type;
        public readonly string Lexeme;
        public readonly object Literal;

        public readonly SourceLocation SourceLocation;

        public Token Previous { get; private set; }
        public Token Next { get; set; }

        public Token(TokenType type, string lexeme, object literal, SourceLocation sourceLocation, Token previous)
        {
            Type = type;
            Lexeme = lexeme;
            Literal = literal;
            SourceLocation = sourceLocation;
            Previous = previous;
        }

        public Token InsertBefore(TokenType type, string lexeme, object literal)
        {
            Token token = new Token(type, lexeme, literal, SourceLocation, Previous);
            token.Next = this;

            return token;
        }

        public Token InsertAfter(TokenType type, string lexeme, object literal)
        {
            Token token = new Token(type, lexeme, literal, SourceLocation, this);
            token.Next = this.Next;

            return token;
        }

        public void Replace(Token newToken)
        {
            newToken.Next = Next;
            newToken.Previous = Previous;

            if(Previous != null)
                Previous.Next = newToken;

            if(Next != null)
                Next.Previous = newToken;
        }

        public override string ToString()
        {
            return $"{{{Type} {Lexeme} {Literal}}}";
        }
    }

}
