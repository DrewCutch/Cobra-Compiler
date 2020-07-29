using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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


        public static int GetWhiteSpaceBetween(Token a, Token b)
        {
            if (a.SourceLocation.Line != b.SourceLocation.Line)
                return 0;

            return (b.SourceLocation.CharIndex - a.SourceLocation.CharIndex) - a.Lexeme.Length;
        }

        public static int GetLinesBetween(Token a, Token b)
        {
            return b.SourceLocation.Line - a.SourceLocation.Line;
        }

        public static string Stringify(Token[] tokens)
        {
            if (tokens.Length == 0)
                return "";

            StringBuilder builder = new StringBuilder();

            Token last = tokens[0];
            builder.Append(last.Lexeme);

            for (int i = 1; i < tokens.Length; i++)
            {
                Token next = tokens[i];

                builder.Append('\n', GetLinesBetween(last, next));
                builder.Append(' ', GetWhiteSpaceBetween(last, next));
                builder.Append(next.Lexeme);

                last = next;
            }

            return builder.ToString();
        }

        public static string GetWholeLine(Token token)
        {
            int line = token.SourceLocation.Line;

            LinkedList<Token> tokens = new LinkedList<Token>();
            tokens.AddFirst(token);

            StringBuilder builder = new StringBuilder();

            Token first = token;

            while (first.Previous != null && first.Previous.SourceLocation.Line == line)
            {
                first = first.Previous;
                tokens.AddFirst(first);
            }

            builder.Append(' ', first.SourceLocation.CharIndex);

            Token last = token;

            while (last.Next != null && last.Next.SourceLocation.Line == line)
            {
                last = last.Next;
                tokens.AddLast(last);
            }

            builder.Append(Stringify(tokens.ToArray()));

            return builder.ToString();
        }
    }

}
