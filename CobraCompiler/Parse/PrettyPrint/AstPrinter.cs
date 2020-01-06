using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;

namespace CobraCompiler.Parse.PrettyPrint
{
    class AstPrinter: IExpressionTraverser<bool>
    {
        private readonly TreePrinter _printer;

        public AstPrinter()
        {
            _printer = new TreePrinter();
        }

        public void PrintExpression(Expression expr)
        {
            _printer.Reset();
            expr.Accept(this, true);
            Console.WriteLine(_printer.Diagram);
        }

        public void PrintStatements(IReadOnlyList<Statement> statements)
        {
            _printer.Reset();
            GenerateTree(statements);
            Console.WriteLine(_printer.Diagram);
        }

        private void GenerateTree(Statement statement)
        {
            GenerateTree(new List<Statement>(){statement});
        }

        private void GenerateTree(IReadOnlyList<Statement> statements)
        {
            if (statements.Count == 0)
                return;

            Statement last = statements.Last();
            foreach (Statement statement in statements)
            {
                bool onLast = statement == last;

                switch (statement)
                {
                    case BlockStatement blockStatement:
                        _printer.AddNode("Block", onLast);
                        GenerateTree(blockStatement.Body);
                        _printer.ExitNode();
                        break;
                    case ExpressionStatement expressionStatement:
                        expressionStatement.Expression.Accept(this, onLast);
                        break;
                    case FuncDeclarationStatement funcDeclarationStatement:
                        _printer.AddNode($"Func {funcDeclarationStatement.Name.Lexeme}", onLast);
                        _printer.AddNode("Params", false);
                        GenerateTree(funcDeclarationStatement.Params);
                        _printer.ExitNode();
                        _printer.AddNode("Body", true);
                        GenerateTree(funcDeclarationStatement.Body);
                        _printer.ExitNode();
                        _printer.ExitNode();
                        break;
                    case ParamDeclarationStatement paramDeclarationStatement:
                        _printer.AddLeaf($"{paramDeclarationStatement.Name.Lexeme}: {paramDeclarationStatement.TypeInit.IdentifierStr}", onLast);
                        break;
                    case ReturnStatement returnStatement:
                        _printer.AddNode("Return", onLast);
                        returnStatement.Value.Accept(this, onLast);
                        _printer.ExitNode();
                        break;
                    case VarDeclarationStatement varDeclarationStatement:
                        _printer.AddNode($"Declare {varDeclarationStatement.Name.Lexeme}: {varDeclarationStatement.TypeInit.IdentifierStr}", onLast);
                        varDeclarationStatement.Assignment?.Accept(this, true);
                        _printer.ExitNode();
                        break;
                    case IfStatement ifStatement:
                        _printer.AddNode("If", onLast);
                        _printer.AddNode("Condition", false);
                        ifStatement.Condition.Accept(this, true);
                        _printer.ExitNode();
                        _printer.AddNode("Then", ifStatement.Else == null);
                        GenerateTree(ifStatement.Then);
                        _printer.ExitNode();

                        if (ifStatement.Else != null)
                        {
                            _printer.AddNode("Else", true);
                            GenerateTree(ifStatement.Else);
                            _printer.ExitNode();
                        }
                        _printer.ExitNode();
                        break;
                    case WhileStatement whileStatement:
                        _printer.AddNode("While", onLast);
                        _printer.AddNode("Condition", false);
                        whileStatement.Condition.Accept(this, true);
                        _printer.ExitNode();
                        _printer.AddNode("Body", whileStatement.Else == null);
                        GenerateTree(whileStatement.Body);
                        _printer.ExitNode();

                        if (whileStatement.Else != null)
                        {
                            _printer.AddNode("Else", true);
                            GenerateTree(whileStatement.Else);
                            _printer.ExitNode();
                        }
                        _printer.ExitNode();
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public void Visit(AssignExpression expr, bool onLast)
        {
            _printer.AddNode("Assign", onLast);
            expr.Value.Accept(this, true);
            _printer.ExitNode();
        }

        public void Visit(BinaryExpression expr, bool onLast)
        {
            _printer.AddNode($"Op {expr.Op.Lexeme}", onLast);
            expr.Left.Accept(this, false);
            expr.Right.Accept(this, true);
            _printer.ExitNode();
        }

        public void Visit(CallExpression expr, bool onLast)
        {
            _printer.AddNode("Call", onLast);
            expr.Callee.Accept(this, false);
            for (int i = 0; i < expr.Arguments.Count; i++)
                expr.Arguments[i].Accept(this, i == expr.Arguments.Count - 1);
            _printer.ExitNode();
        }

        public void Visit(LiteralExpression expr, bool onLast)
        {
            _printer.AddLeaf($"Literal: {expr.Value}", onLast);
        }

        public void Visit(TypeInitExpression expr, bool onLast)
        {
            throw new NotImplementedException();
        }

        public void Visit(UnaryExpression expr, bool onLast)
        {
            _printer.AddNode($"Op {expr.Op.Lexeme}", onLast);
            expr.Right.Accept(this, true);
            _printer.ExitNode();
        }

        public void Visit(GetExpression expr, bool onLast)
        {
            _printer.AddNode($".{expr.Name.Lexeme}", onLast);
            expr.Obj.Accept(this, true);
            _printer.ExitNode();
        }

        public void Visit(GroupingExpression expr, bool onLast)
        {
            _printer.AddNode("Paren", onLast);
            expr.Inner.Accept(this, true);
            _printer.ExitNode();
        }

        public void Visit(VarExpression expr, bool onLast)
        {
            _printer.AddLeaf(expr.Name.Lexeme, onLast);
        }
    }
}
