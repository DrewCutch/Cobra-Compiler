using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.ErrorLogging;

namespace CobraCompiler.Scanning
{
    public class Scanner
    {
        private readonly SourceReader _sourceReader;
        private readonly ErrorLogger _errorLogger;

        public Scanner(SourceReader sourceReader, ErrorLogger errorLogger)
        {
            _sourceReader = sourceReader;
            _errorLogger = errorLogger;
        }

        public List<Token> GetTokens()
        {
            List<Token> tokens = new List<Token>();

            string path = _sourceReader.Path;
            int lineNumber = 1;

            foreach (string line in _sourceReader.Lines())
            {
                StringNibbler lineNibbler = new StringNibbler(line);
                String currentLexeme = "";

                while (lineNibbler.HasNext())
                {
                    int charIndex = lineNibbler.Pos;
                    TokenType ? nextTokenType = null;
                    try
                    {
                        while ((nextTokenType = getTokenType(currentLexeme, lineNibbler.Peek(), new SourceLocation(path, lineNumber, charIndex - currentLexeme.Length), tokens.LastOrDefault())) == null)
                        {
                            char nextChar = lineNibbler.Pop();

                            if (nextChar != ' ' && nextChar != '\t' || IsPotentialStrLiteral(currentLexeme))
                                currentLexeme += nextChar;
                            
                            charIndex += 1;

                            if (currentLexeme == "//")
                                goto endOfLine; // Stop scanning this line

                            if (currentLexeme == "" && lineNibbler.Peek() == '\0') // If line has trailing whitespace
                                break;
                        }
                    }
                    catch (ScanningException scanningException)
                    {
                        _errorLogger.Log(scanningException);
                        nextTokenType = TokenType.Invalid;
                    }

                    if (nextTokenType is TokenType _nextTokenType)
                    {
                        Token next = new Token(_nextTokenType, currentLexeme, getValue(_nextTokenType, currentLexeme),
                            new SourceLocation(path, lineNumber, charIndex - currentLexeme.Length), tokens.LastOrDefault());
                        if(tokens.Count > 0) 
                            tokens.Last().Next = next;

                        tokens.Add(next);

                        currentLexeme = "";
                    }
                }

                if (currentLexeme.Length > 0)
                    _errorLogger.Log(new ScanningException(new Token(TokenType.Invalid, currentLexeme, null, new SourceLocation(path, lineNumber, lineNibbler.Pos), tokens.LastOrDefault())));

                endOfLine:

                Token newLine = new Token(TokenType.NewLine, "", null, new SourceLocation(path, lineNumber, lineNibbler.Pos), tokens.LastOrDefault());
                if (tokens.Count > 0)
                    tokens.Last().Next = newLine;

                tokens.Add(newLine);

                lineNumber++;
            }

            Token endOfFile = new Token(TokenType.Eof, "", null, new SourceLocation(path, lineNumber, 0), tokens.LastOrDefault());
            if (tokens.Count > 0)
                tokens.Last().Next = endOfFile;

            tokens.Add(endOfFile);
            return tokens;
        }

        private TokenType? getTokenType(string lexeme, char next, SourceLocation sourceLocation, Token lastToken)
        {
            switch (lexeme)
            {
                case "&":
                    return TokenType.Ampersand;
                case "|":
                    return TokenType.Bar;
                case "(":
                    return TokenType.LeftParen;
                case ")":
                    return TokenType.RightParen;
                case "{":
                    return TokenType.LeftBrace;
                case "}":
                    return TokenType.RightBrace;
                case "[":
                    return TokenType.LeftBracket;
                case "]":
                    return TokenType.RightBracket;
                case ",":
                    return TokenType.Comma;
                case ".":
                    return TokenType.Dot;
                case "-":
                    return TokenType.Minus;
                case "+":
                    return TokenType.Plus;
                case ":":
                    return TokenType.Colon;
                case "*":
                    return TokenType.Star;
                case "/":
                    if(next != '/')
                        return TokenType.Slash;
                    return null;
                case "!":
                    if (next != '=')
                        return TokenType.Bang;
                    return null;
                case "!=":
                    return TokenType.BangEqual;
                case "=":
                    if (next != '=')
                        return TokenType.Equal;
                    return null;
                case "==":
                    return TokenType.EqualEqual;
                case ">":
                    if (next != '=')
                        return TokenType.Greater;
                    return null;
                case ">=":
                    return TokenType.GreaterEqual;
                case "<":
                    if (next != '=')
                        return TokenType.Less;
                    return null;
                case "<=":
                    return TokenType.LessEqual;
                case "and":
                    if(!isAlphanumeric(next))
                        return TokenType.And;
                    return null;
                case "class":
                    if (!isAlphanumeric(next))
                        return TokenType.Class;
                    return null;
                case "else":
                    if (!isAlphanumeric(next))
                        return TokenType.Else;
                    return null;
                case "false":
                    if (!isAlphanumeric(next))
                        return TokenType.False;
                    return null;
                case "func":
                    if (!isAlphanumeric(next))
                        return TokenType.Func;
                    return null;
                case "for":
                    if (!isAlphanumeric(next))
                        return TokenType.For;
                    return null;
                case "if":
                    if (!isAlphanumeric(next))
                        return TokenType.If;
                    return null;
                case "import":
                    if (!isAlphanumeric(next))
                        return TokenType.Import;
                    return null;
                case "in":
                    if (!isAlphanumeric(next))
                        return TokenType.In;
                    return null;
                case "init":
                    if (!isAlphanumeric(next))
                        return TokenType.Init;
                    return null;
                case "null":
                    if (!isAlphanumeric(next))
                        return TokenType.Null;
                    return null;
                case "or":
                    if (!isAlphanumeric(next))
                        return TokenType.Or;
                    return null;
                case "op":
                    if (!isAlphanumeric(next))
                        return TokenType.Op;
                    return null;
                case "return":
                    if (!isAlphanumeric(next))
                        return TokenType.Return;
                    return null;
                case "super":
                    if (!isAlphanumeric(next))
                        return TokenType.Super;
                    return null;
                case "true":
                    if (!isAlphanumeric(next))
                        return TokenType.True;
                    return null;
                case "type":
                    if (!isAlphanumeric(next))
                        return TokenType.Type;
                    return null;
                case "var":
                    if (!isAlphanumeric(next))
                        return TokenType.Var;
                    return null;
                case "while":
                    if (!isAlphanumeric(next))
                        return TokenType.While;
                    return null;
                default:
                    if (isQuoted(lexeme))
                        return TokenType.String;
                    if (isInteger(lexeme) && !isAlphanumeric(next) && next != '.')
                        return TokenType.Integer;
                    if (isFloat(lexeme) && !isAlphanumeric(next))
                       return TokenType.Decimal;
                    if (isAlphanumeric(lexeme) && !isAlphanumeric(next) && !isInteger(lexeme))
                        return TokenType.Identifier;
                    if(lexeme.Length > 0 && (next == '\0' || (next == ' ' && !IsPotentialStrLiteral(lexeme))))
                        throw new ScanningException(new Token(TokenType.Invalid, lexeme, null, sourceLocation, lastToken));
                    return null;
            }
        }

        private bool isWhitespace(char chr)
        {
            return Char.IsWhiteSpace(chr);
        }

        private bool isAlphanumeric(string str)
        {
            return str.Replace("_", string.Empty).All(Char.IsLetterOrDigit) && str.Length > 0;
        }

        private bool isAlphanumeric(char chr)
        {
            return Char.IsLetterOrDigit(chr) || chr == '_';
        }

        private bool isFloat(string str)
        {
            bool decimalReached = false;

            if (str.Length == 0)
                return false;

            foreach(char chr in str)
            {
                if (chr == '.' && !decimalReached)
                    decimalReached = true;
                else if (!Char.IsDigit(chr))
                    return false;
            }

            return decimalReached && str.Last() != '.';
        }

        private bool IsPotentialStrLiteral(string str)
        {
            return str.Length > 0 && str[0] == '"';
        }

        private object getValue(TokenType tokenType, string lexeme)
        {
            switch (tokenType)
            {
                case TokenType.String:
                    return lexeme.Substring(1, lexeme.Length - 2);
                case TokenType.Integer:
                    return int.Parse(lexeme);
                case TokenType.Decimal:
                    return float.Parse(lexeme);
                default:
                    return null;
            }
        }

        private bool isInteger(string str)
        {
            return str.All(Char.IsDigit) && str.Length > 0;
        }

        private bool isQuoted(string str)
        {
            return str.Length >= 2 && str[0] == '"' && str.Last() == '"';
        }

        private bool isAlphanumericOrSpace(char chr)
        {
            return Char.IsLetterOrDigit(chr) || chr == ' ';
        }
    }
}
