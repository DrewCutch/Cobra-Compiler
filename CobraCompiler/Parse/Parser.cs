using System;
using System.Collections.Generic;
using CobraCompiler.ErrorLogging;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Parse.TypeCheck;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse
{
    class Parser
    {
        private readonly ListNibbler<Token> _tokens;
        private readonly ErrorLogger _errorLogger;

        public Parser(IReadOnlyList<Token> tokens, ErrorLogger errorLogger)
        {
            this._tokens = new ListNibbler<Token>(tokens);
            this._errorLogger = errorLogger;
        }

        public List<Statement> Parse()
        {
            List<Statement> statements = new List<Statement>();
            while (_tokens.HasNext() && _tokens.Peek().Type != TokenType.Eof)
            {
                try
                {
                    Statement nextStatement = Definition();
                    if(nextStatement != null)
                        statements.Add(nextStatement);
                }
                catch (ParsingException parsingException)
                {
                    _errorLogger.Log(parsingException);
                    _tokens.Pop();
                }
            }

            return statements;
        }

        private Statement Definition()
        {
            if(Match(TokenType.Func))
                return FuncDeclaration();

            if (Match(TokenType.NewLine))
                return null;

            throw new ParsingException(_tokens.Peek(0), "Invalid code outside of function");
        }

        private Statement Declaration()
        {
            if (Match(TokenType.Var))
                return VarDeclaration();

            if (Match(TokenType.Func))
                return FuncDeclaration();

            if (Match(TokenType.NewLine))
                return null;

            return Statement();
        }

        private Statement VarDeclaration()
        {
            Token name = Expect(TokenType.Identifier, "Expect variable name.");
            Expect(TokenType.Colon, "Expect colon after variable declaration.");

            TypeInitExpression typeInit = InitType();

            AssignExpression initializer = null;
            if (Match(TokenType.Equal))
            {
                initializer = new AssignExpression(name, Expression());
            }

            Expect(TokenType.NewLine, "Expect new line after variable declaration.");

            return new VarDeclarationStatement(name, typeInit, initializer);
        }

        private Statement FuncDeclaration()
        {
            Token name = Expect(TokenType.Identifier, "Expect function name.");

            Expect(TokenType.LeftParen, "Expect '(' after function name.");

            List<ParamDeclarationStatement> paramDeclarations = new List<ParamDeclarationStatement>();

            if (!Check(TokenType.RightParen))
            {
                do
                {
                    paramDeclarations.Add(ParamDeclaration());
                } while (Match(TokenType.Comma));
            }

            Expect(TokenType.RightParen, "Expect ')' after parameters.");

            Token? returnType = null;
            if (Match(TokenType.Colon))
            {
                returnType = Expect(TokenType.Identifier, "Expect return type after colon.");
            }

            Match(TokenType.NewLine);

            return new FuncDeclarationStatement(name, paramDeclarations, returnType, Statement());
        }

        private ParamDeclarationStatement ParamDeclaration()
        {
            Token name = Expect(TokenType.Identifier, "Expect parameter name.", ignoreNewline:true);
            Expect(TokenType.Colon, "Expect colon after parameter name.");
            TypeInitExpression typeInit = InitType();

            return new ParamDeclarationStatement(name, typeInit);
        }

        private TypeInitExpression InitType()
        {
            List<Token> typeIdentifier = new List<Token>();

            typeIdentifier.Add(Expect(TokenType.Identifier, "Expect type identifier after colon."));

            while (Match(TokenType.Dot))
            {
                typeIdentifier.Add(Expect(TokenType.Identifier, "Expect identifier after '.'"));
            }

            return new TypeInitExpression(typeIdentifier);
        }

        private Statement Statement()
        {
            if(Match(TokenType.LeftBrace))
                return Block();

            if (Match(TokenType.Return))
                return Return();

            if (Match(TokenType.If))
                return IfStatement();

            return ExpressionStatement();
        }

        private Statement Block()
        {
            List<Statement> body = new List<Statement>();

            while (!Check(TokenType.RightBrace) && _tokens.HasNext() && _tokens.Peek().Type != TokenType.Eof)
            {
                Statement nextStatement = Declaration();
                if (nextStatement != null)
                    body.Add(nextStatement);
            }

            Expect(TokenType.RightBrace, "Expect '}' after code block.");
            return new BlockStatement(body);
        }

        private Statement Return()
        {
            Token keyword = _tokens.Previous();
            Expression expr = Expression();
            Expect(TokenType.NewLine, "Expect newline after return statement");
            return new ReturnStatement(keyword, expr);
        }

        private Statement IfStatement()
        {
            Expect(TokenType.LeftParen, "Expect '(' after 'if'.", ignoreNewline:true);
            Expression condition = Expression();
            Expect(TokenType.RightParen, "Expect ')' after 'if' condition.", ignoreNewline: true);

            Statement thenStatement = Statement();
            Statement elseStatement = null;

            if (MatchIgnoringNewline(TokenType.Else))
                elseStatement = Statement();

            return new IfStatement(condition, thenStatement, elseStatement);
        }

        private Statement ExpressionStatement()
        {
            Expression expr = Expression();
            Expect(TokenType.NewLine, "Expect newline after expression");
            return new ExpressionStatement(expr);
        }

        private Expression Expression()
        {
            return Assignment();
        }

        private Expression Assignment()
        {
            Expression expr = Equality();

            if (Match(TokenType.Equal))
            {
                Token equals = _tokens.Previous();
                Expression value = Assignment();

                if (expr is VarExpression varExpression)
                {
                    Token name = varExpression.Name;
                    return new AssignExpression(name, value);
                }

                throw new ParsingException(equals, "Invalid assignment target.");
            }

            return expr;
        }

        private Expression Equality()
        {
            Expression expr = Comparison();

            while (Match(TokenType.BangEqual, TokenType.EqualEqual))
            {
                Token op = _tokens.Previous();
                Expression right = Comparison();
                expr = new BinaryExpression(expr, op, right);
            }

            return expr;
        }

        private Expression Comparison()
        {
            Expression expr = Addition();

            while (Match(TokenType.Less, TokenType.LessEqual, TokenType.Greater, TokenType.GreaterEqual))
            {
                Token op = _tokens.Previous();
                Expression right = Addition();
                expr = new BinaryExpression(expr, op, right);
            }

            return expr;
        }

        private Expression Addition()
        {
            Expression expr = Multiplication();

            while (Match(TokenType.Plus, TokenType.Minus))
            {
                Token op = _tokens.Previous();
                Expression right = Multiplication();
                expr = new BinaryExpression(expr, op, right);
            }

            return expr;
        }

        private Expression Multiplication()
        {
            Expression expr = Unary();

            while (Match(TokenType.Slash, TokenType.Star))
            {
                Token op = _tokens.Previous();
                Expression right = Unary();
                expr = new BinaryExpression(expr, op, right);
            }

            return expr;
        }

        private Expression Unary()
        {
            if (Match(TokenType.Bang, TokenType.Minus))
            {
                Token op = _tokens.Previous();
                Expression right = Unary();
                return new UnaryExpression(op, right);
            }

            return Postfix();
        }

        private Expression Postfix()
        {
            Expression expr = Primary();

            while (true)
            {
                if (Match(TokenType.LeftParen))
                {
                    expr = FinishCall(expr);
                }
                else if (Match(TokenType.Dot))
                {
                    Token name = Expect(TokenType.Identifier, "Expect property name after '.'");
                    expr = new GetExpression(expr, name);
                }
                else
                {
                    break;
                }
            }

            return expr;
        }

        private Expression Primary()
        {
            if (Match(TokenType.False)) return new LiteralExpression(false, DotNetCobraType.Bool);
            if (Match(TokenType.True)) return new LiteralExpression(true, DotNetCobraType.Bool);
            if (Match(TokenType.Null)) return new LiteralExpression(null, DotNetCobraType.Null);

            if(Match(TokenType.Integer)) return new LiteralExpression(_tokens.Previous().Literal, DotNetCobraType.Int);
            if (Match(TokenType.Decimal)) return new LiteralExpression(_tokens.Previous().Literal, DotNetCobraType.Float);
            if (Match(TokenType.String)) return new LiteralExpression(_tokens.Previous().Literal, DotNetCobraType.Str);

            if (Match(TokenType.Identifier))
            {
                return new VarExpression(_tokens.Previous());
            }

            if (Match(TokenType.LeftParen))
            {
                Expression expr = Expression();
                Expect(TokenType.RightParen, "Expect ')' after expression.");
                return new GroupingExpression(expr);
            }

            if (Match(TokenType.NewLine))
            {
                return null;
            }

            throw new ParsingException(_tokens.Peek(), "Expect expression.");
        }

        private Expression FinishCall(Expression callee)
        {
            List<Expression> arguments = new List<Expression>();

            if (!Check(TokenType.RightParen))
            {
                do
                {
                    arguments.Add(Expression());
                } while (Match(TokenType.Comma));
            }

            Token paren = Expect(TokenType.RightParen, "Expect closing paren after arguments.");

            return new CallExpression(callee, paren, arguments);
        }

        private Token Expect(TokenType type, String message, bool ignoreNewline=false)
        {
            if (ignoreNewline)
            {
                while (Check(TokenType.NewLine))
                    _tokens.Pop();
            }

            if (Check(type))
                return _tokens.Pop();

            throw new ParsingException(_tokens.Peek(), message);
        }

        private bool MatchIgnoringNewline(params TokenType[] types)
        {
            while (Check(TokenType.NewLine))
                _tokens.Pop();

            return Match(types);
        }

        private bool Match(params TokenType[] types)
        {
            foreach (TokenType type in types)
            {
                if (Check(type))
                {
                    _tokens.Pop();
                    return true;
                }
            }

            return false;
        }

        private bool Check(TokenType type)
        {
            if (!_tokens.HasNext())
                return false;
            return _tokens.Peek().Type == type;
        }
    }
}
