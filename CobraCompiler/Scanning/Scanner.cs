using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.ErrorLogging;

namespace CobraCompiler.Scanning
{
    class Scanner
    {
        private readonly FileReader _fileReader;
        private readonly ErrorLogger _errorLogger;

        public Scanner(FileReader fileReader, ErrorLogger errorLogger)
        {
            _fileReader = fileReader;
            _errorLogger = errorLogger;
        }

        public List<Token> GetTokens()
        {
            List<Token> tokens = new List<Token>();
            int lineNumber = 0;

            foreach (string line in _fileReader.Lines())
            {
                StringNibbler lineNibbler = new StringNibbler(line);
                String currentLexeme = "";

                while (lineNibbler.HasNext())
                {
                    TokenType? nextTokenType = null;
                    try
                    {
                        while ((nextTokenType =
                                   getTokenType(currentLexeme.ToString(), lineNibbler.Peek(), lineNumber)) == null)
                        {
                            currentLexeme += lineNibbler.Pop();
                            if(!IsPotentialStrLiteral(currentLexeme))
                                currentLexeme = currentLexeme.Trim();

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
                        tokens.Add(new Token(_nextTokenType, currentLexeme, getValue(_nextTokenType, currentLexeme), lineNumber));
                        currentLexeme = "";
                    }
                }

                if (currentLexeme.Length > 0)
                    _errorLogger.Log(new ScanningException(currentLexeme, lineNumber));

                endOfLine:

                tokens.Add(new Token(TokenType.NewLine, "", null, lineNumber));
                lineNumber++;
            }

            tokens.Add(new Token(TokenType.Eof, "", null, lineNumber));
            return tokens;
        }

        private TokenType? getTokenType(string lexeme, char next, int lineNumber)
        {
            switch (lexeme)
            {
                case "(":
                    return TokenType.LeftParen;
                case ")":
                    return TokenType.RightParen;
                case "{":
                    return TokenType.LeftBrace;
                case "}":
                    return TokenType.RightBrace;
                case "[":
                    return TokenType.RightBracket;
                case "]":
                    return TokenType.LeftBracket;
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
                case "in":
                    if (!isAlphanumeric(next))
                        return TokenType.In;
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
                case "this":
                    if (!isAlphanumeric(next))
                        return TokenType.This;
                    return null;
                case "true":
                    if (!isAlphanumeric(next))
                        return TokenType.True;
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
                        throw new ScanningException(lexeme, lineNumber);
                    return null;
            }
        }

        private bool isWhitespace(char chr)
        {
            return Char.IsWhiteSpace(chr);
        }

        private bool isAlphanumeric(string str)
        {
            return str.All(Char.IsLetterOrDigit) && str.Length > 0;
        }

        private bool isAlphanumeric(char chr)
        {
            return Char.IsLetterOrDigit(chr);
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
