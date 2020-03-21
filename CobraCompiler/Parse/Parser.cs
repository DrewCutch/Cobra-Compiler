using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using CobraCompiler.ErrorLogging;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Parse.TypeCheck;
using CobraCompiler.Parse.TypeCheck.Types;
using CobraCompiler.Scanning;

namespace CobraCompiler.Parse
{
    class Parser
    {
        private readonly ListNibbler<Token> _tokens;
        private readonly ErrorLogger _errorLogger;
        private bool IsAtEnd => !_tokens.HasNext() || _tokens.Peek().Type == TokenType.Eof;

        public Parser(IReadOnlyList<Token> tokens, ErrorLogger errorLogger)
        {
            this._tokens = new ListNibbler<Token>(tokens);
            this._errorLogger = errorLogger;
        }

        public List<Statement> Parse()
        {
            List<Statement> statements = new List<Statement>();
           
            while (!IsAtEnd)
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
                    sync();
                }
            }

            return statements;
        }

        private void sync()
        {
            while (!IsAtEnd)
            {
                if (Match(TokenType.NewLine))
                    return;
                _tokens.Pop();
            }
        }

        private Statement Definition()
        {
            if(Match(TokenType.Func))
                return FuncDeclaration();

            if (Match(TokenType.Import))
                return Import();

            if (Match(TokenType.Type))
                return TypeDeclaration();

            if (Match(TokenType.NewLine))
                return null;

            throw new ParsingException(_tokens.Peek(0), "Invalid code outside of function");
        }

        private Statement Declaration()
        {
            try
            {
                if (Match(TokenType.Var))
                    return VarDeclaration();

                if (Match(TokenType.Func))
                    return FuncDeclaration();

                if (Match(TokenType.NewLine))
                    return null;

                return Statement();
            }
            catch (ParsingException parsingException)
            {
                _errorLogger.Log(parsingException);
                _tokens.Pop();
                sync();
                return new InvalidStatement();
            }
        }

        private Statement VarDeclaration()
        {
            Token name = Expect(TokenType.Identifier, "Expect variable name.");
            Expect(TokenType.Colon, "Expect colon after variable declaration.");

            TypeInitExpression typeInit = TypeInit();

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

            TypeInitExpression returnType = null;
            if (Match(TokenType.Colon))
            {
                returnType = TypeInit();
            }

            Match(TokenType.NewLine);

            return new FuncDeclarationStatement(name, paramDeclarations, returnType, Statement());
        }

        private ImportStatement Import()
        {
            Queue<Token> names = new Queue<Token>();
            Token keyword = _tokens.Previous();

            while (true)
            {
                if (Match(TokenType.Identifier))
                    names.Enqueue(_tokens.Previous());
                else if (Match(TokenType.NewLine))
                    break;
                else if (!Match(TokenType.Dot))
                    throw new ParsingException(_tokens.Peek(0), "Invalid import target");
            }

            Expression identifierExpression = new VarExpression(names.Dequeue());

            while (names.Count > 0)
            {
                Token name = names.Dequeue();

                identifierExpression = new GetExpression(identifierExpression, name);
            }

            return new ImportStatement(keyword, identifierExpression);
        }

        private Statement TypeDeclaration()
        {
            Token name = Expect(TokenType.Identifier, "Expect type name.");
            Expect(TokenType.Colon, "Expect colon after type declaration.");

            TypeInitExpression typeInit = TypeInit();

            Expect(TokenType.NewLine, "Expect new line after type declaration.");

            return new TypeDeclarationStatement(name, typeInit);
        }

        private ParamDeclarationStatement ParamDeclaration()
        {
            Token name = Expect(TokenType.Identifier, "Expect parameter name.", ignoreNewline:true);
            Expect(TokenType.Colon, "Expect colon after parameter name.");
            TypeInitExpression typeInit = TypeInit();

            return new ParamDeclarationStatement(name, typeInit);
        }

        private TypeInitExpression SimpleTypeInit()
        {
            List<Token> typeIdentifier = new List<Token>();
            List<TypeInitExpression> genericParams = new List<TypeInitExpression>();

            if(Check(TokenType.LeftBracket)) // List Type literal special case
                typeIdentifier.Add(new Token(TokenType.Identifier, "list", null, _tokens.Previous().Line));
            else
                typeIdentifier.Add(Expect(TokenType.Identifier, "Expect type identifier."));

            while (Match(TokenType.Dot))
            {
                typeIdentifier.Add(Expect(TokenType.Identifier, "Expect identifier after '.'"));
            }

            if (Match(TokenType.LeftBracket))
            {
                do
                {
                    genericParams.Add(TypeInit());
                } while (Match(TokenType.Comma));

                Expect(TokenType.RightBracket, "Expect closing ']' after generic parameters");
            }
            

            return new TypeInitExpression(typeIdentifier, genericParams);
        }

        private TypeInitExpression TypeInit()
        {
            Expression typeInit = Union();
            return ResolveType(typeInit);
        }

        private TypeInitExpression ResolveType(Expression expr)
        {
            Expression typeExpression = expr;
            if (typeExpression is TypeInitExpression simpleTypeInit)
                return simpleTypeInit;

            if(!(typeExpression is BinaryExpression))
                throw new NotImplementedException();
            
            BinaryExpression typeBinary = typeExpression as BinaryExpression;
            switch (typeBinary.Op.Type)
            {
                case TokenType.Bar:
                    TypeInitExpression left = ResolveType(typeBinary.Left);
                    TypeInitExpression right = ResolveType(typeBinary.Right);
                    return new TypeInitExpression(new Token[]{new Token(TokenType.Identifier, "union", null, typeBinary.Op.Line)}, new TypeInitExpression[] {left, right});
            }

            throw new NotImplementedException();
        }

        private Expression Union()
        {
            Expression expr = Intersect();

            while (Match(TokenType.Bar))
            {
                Token op = _tokens.Previous();
                Expression right = Intersect();
                expr = new BinaryExpression(expr, op, right);
            }

            return expr;
        }

        private Expression Intersect()
        {
            Expression expr = SimpleTypeInit();

            while (Match(TokenType.Ampersand))
            {
                Token op = _tokens.Previous();
                Expression right = SimpleTypeInit();
                expr = new BinaryExpression(expr, op, right);
            }

            return expr;
        }



        private Statement Statement()
        {
            if(Match(TokenType.LeftBrace))
                return Block();

            if (Match(TokenType.Return))
                return Return();

            if (Match(TokenType.If))
                return IfStatement();

            if (Match(TokenType.While))
                return WhileStatement();

            return ExpressionStatement();
        }

        private Statement Block()
        {
            List<Statement> body = new List<Statement>();

            while (!Check(TokenType.RightBrace) && !IsAtEnd)
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
            Expression expr = Expression() ?? new LiteralExpression(null, DotNetCobraType.Unit);
            // Expect(TokenType.NewLine, "Expect newline after return statement");
            return new ReturnStatement(keyword, expr);
        }

        /*
        private Statement ForStatement()
        {
            Expect(TokenType.LeftParen, "Expect '(' after 'for'.", ignoreNewline: true);
            Statement init = MatchIgnoringNewline(TokenType.Var) ? VarDeclaration() : ExpressionStatement();
            // Expect(TokenType.SemiColon, "Expec ';' after init statement in 'for'", ignoreNewline: true);
            Expression condition = Expression();
            // Expect(TokenType.SemiColon, "Expec ';' after condition in 'for'", ignoreNewline: true);
            Statement after = ExpressionStatement();
            Expect(TokenType.RightParen, "Expect ')' after 'for' condition.", ignoreNewline: true);

            IgnoreNewlines();

            Statement bodyStatement = Statement();
            // TODO: convert for statement to while statement
            Statement elseStatement = null;

            if (MatchIgnoringNewline(TokenType.Else))
            {
                IgnoreNewlines();
                elseStatement = Statement();
            }

            return new WhileStatement(condition, bodyStatement, elseStatement);
        }
        */
        private Statement WhileStatement()
        {
            Expect(TokenType.LeftParen, "Expect '(' after 'while'.", ignoreNewline: true);
            Expression condition = Expression();
            Expect(TokenType.RightParen, "Expect ')' after 'while' condition.", ignoreNewline: true);

            IgnoreNewlines();

            Statement bodyStatement = Statement();
            Statement elseStatement = null;

            if (MatchIgnoringNewline(TokenType.Else))
            {
                IgnoreNewlines();
                elseStatement = Statement();
            }

            return new WhileStatement(condition, bodyStatement, elseStatement);
        }

        private Statement IfStatement()
        {
            Expect(TokenType.LeftParen, "Expect '(' after 'if'.", ignoreNewline:true);
            Expression condition = Expression();
            Expect(TokenType.RightParen, "Expect ')' after 'if' condition.", ignoreNewline: true);

            IgnoreNewlines();

            Statement thenStatement = Statement();
            Statement elseStatement = null;

            if (MatchIgnoringNewline(TokenType.Else))
            {
                IgnoreNewlines();
                elseStatement = Statement();
            }

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
                else if (Match(TokenType.LeftBracket))
                {
                    List<Expression> indicies = new List<Expression>();
                    do
                    {
                        indicies.Add(Expression());
                    } while (Match(TokenType.Comma));
                    expr = new IndexExpression(expr, indicies);
                    Expect(TokenType.RightBracket, "Expect ']' after indices");
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

            if (Match(TokenType.Integer)) return new LiteralExpression(_tokens.Previous().Literal, DotNetCobraType.Int);
            if (Match(TokenType.Decimal)) return new LiteralExpression(_tokens.Previous().Literal, DotNetCobraType.Float);
            if (Match(TokenType.String)) return new LiteralExpression(_tokens.Previous().Literal, DotNetCobraType.Str);
            if (Check(TokenType.LeftBracket)) return ListLiteral();

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



        private ListLiteralExpression ListLiteral()
        {
            List<Expression> elements = new List<Expression>();

            Expect(TokenType.LeftBracket, "Expect '[' at beginning of list literal");
            do
            {
                elements.Add(Expression());
            } while (Match(TokenType.Comma));

            Expect(TokenType.RightBracket, "Expect closing ']' at end of list literal");

            return new ListLiteralExpression(elements);
        }

        private Token Expect(TokenType type, String message, bool ignoreNewline=false)
        {
            if (ignoreNewline)
                IgnoreNewlines();

            if (Check(type))
                return _tokens.Pop();

            throw new ParsingException(_tokens.Peek(), message);
        }

        private void IgnoreNewlines()
        {
            while (Check(TokenType.NewLine))
                _tokens.Pop();
        }

        private bool MatchIgnoringNewline(params TokenType[] types)
        {
            IgnoreNewlines();

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
