using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Assemble;
using CobraCompiler.ErrorLogging;
using CobraCompiler.Parse;
using CobraCompiler.Parse.CFG;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Scanning;
using CobraCompiler.TypeCheck.Assertion;
using CobraCompiler.TypeCheck.Exceptions;
using CobraCompiler.TypeCheck.Operators;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.TypeCheck
{
    class FuncChecker
    {
        private readonly ExpressionChecker _expressionChecker;

        public FuncChecker()
        {
            _expressionChecker = new ExpressionChecker();
        }

        public FuncScope DefineFunc(FuncDeclarationStatement funcDeclaration, Scope scope)
        {
            if (funcDeclaration.TypeArguments.Count > 0)
                scope = PushGenericScope(funcDeclaration, funcDeclaration.TypeArguments, scope);

            CobraType returnType = funcDeclaration.ReturnType == null
                ? DotNetCobraType.Unit
                : scope.GetType(funcDeclaration.ReturnType);

            if (funcDeclaration is InitDeclarationStatement)
                returnType = scope.GetType((scope as ClassScope).ClassDeclaration.Type);


            List<(string, CobraType)> funcParams = new List<(string, CobraType)>();
            foreach (ParamDeclarationStatement param in funcDeclaration.Params)
            {
                if (!scope.IsTypeDefined(param.TypeInit))
                    throw new TypeNotDefinedException(param.TypeInit);

                funcParams.Add((param.Name.Lexeme, scope.GetType(param.TypeInit)));
            }

            FuncScope funcScope = new FuncScope(scope, funcDeclaration, funcParams, returnType);

            List<CobraType> typeArgs = funcDeclaration.Params.Select(param => scope.GetType(param.TypeInit)).ToList();
            typeArgs.Add(funcScope.ReturnType);

            CobraType funcType = DotNetCobraGeneric.FuncType.CreateGenericInstance(typeArgs);

            scope.AddSubScope(funcScope);

            scope.Declare(funcDeclaration, funcType);

            // Pop the temporary generic scope
            if (funcDeclaration.TypeArguments.Count > 0)
                scope = scope.Parent;

            CFGNode lastNode = BuildCFG(funcScope, funcScope.CFGraph.Root);

            if(lastNode != funcScope.CFGraph.Terminal)
                lastNode.Link(funcScope.CFGraph.Terminal);

            return funcScope;
        }

        public static Scope PushGenericScope(Statement body, IEnumerable<Token> typeArguments, Scope scope)
        {
            Scope genericScope = new GenericScope(scope, body);
            int i = 0;
            foreach (Token typeArg in typeArguments)
            {
                genericScope.DefineType(typeArg.Lexeme, CobraType.GenericPlaceholder(typeArg.Lexeme, i));
                i++;
            }

            scope.AddSubScope(genericScope);

            return genericScope;
        }

        private static CFGNode BuildCFG(Scope scope, CFGNode root)
        {
            CFGNode previous = root;
            ListNibbler<Statement> statements = new ListNibbler<Statement>(scope.Body);
            while(statements.HasNext())
            {
                Statement statement = statements.Pop();

                if (statement is BlockStatement block)
                {
                    Scope blockScope = new Scope(scope, block.Body.ToArray());
                    scope.AddSubScope(blockScope);

                    if (previous.IsRoot)
                    {
                        CFGNode nextBlock = previous.CreateNext(blockScope);
                        previous = BuildCFG(blockScope, nextBlock);
                    }
                    else
                    {
                        previous = BuildCFG(blockScope, previous);
                    }
                }
                else
                {
                    previous.AddStatement(statement);
                }

                if (statement is IConditionalExpression conditional)
                {
                    Scope thenScope = new Scope(scope, conditional.Then);
                    scope.AddSubScope(thenScope);

                    CFGNode thenNode = previous.CreateNext(thenScope);

                    CFGNode thenEnd = BuildCFG(thenScope, thenNode);

                    CFGNode elseEnd = null;

                    if (conditional.Else != null)
                    {
                        Scope elseScope = new Scope(scope, conditional.Else);
                        scope.AddSubScope(elseScope);

                        CFGNode elseNode = previous.CreateNext(elseScope);

                        elseEnd = BuildCFG(elseScope, elseNode);
                    }

                    CFGNode conditionalEnd = null;

                    if (thenEnd != thenEnd.Graph.Terminal)
                        conditionalEnd = thenEnd.CreateNext(scope);
                    if (elseEnd != null && elseEnd != elseEnd.Graph.Terminal)
                    {
                        if (conditionalEnd == null)
                            conditionalEnd = elseEnd.CreateNext(scope);
                        else
                            elseEnd.Link(conditionalEnd);
                    }
                    else if (elseEnd == null && conditionalEnd == null)
                    {
                        conditionalEnd = previous.CreateNext(scope);
                    }
                    else if (elseEnd == null)
                    {
                        previous.Link(conditionalEnd);
                    }

                    previous = conditionalEnd ?? thenEnd.Graph.Terminal;
                }

                if (statement is GuardStatement guardStatement)
                {
                    Scope elseScope = new Scope(scope, guardStatement.Else);
                    scope.AddSubScope(elseScope);
                    CFGNode elseNode = previous.CreateNext(elseScope);
                    CFGNode elseEnd = BuildCFG(elseScope, elseNode);

                    if(elseEnd != elseEnd.Graph.Terminal)
                        throw new MissingGuardElseReturnException(guardStatement);

                    Scope passScope = new Scope(scope, new BlockStatement(statements.PopRemaining()));
                    scope.AddSubScope(passScope);
                    CFGNode passNode = previous.CreateNext(passScope);

                    previous = BuildCFG(passScope, passNode);
                }

                if (statement is ReturnStatement returnStatement)
                {
                    previous.SetNext(previous.Graph.Terminal);
                    previous = previous.Graph.Terminal;
                }

                if (statement is PanicStatement panicStatement)
                {
                    previous.SetNext(previous.Graph.Terminal);
                    previous = previous.Graph.Terminal;
                }
            }

            return previous;
        }

        public void CheckFunc(FuncScope funcScope, ErrorLogger errorLogger)
        {
            IReadOnlyList<CFGNode> nodes = funcScope.CFGraph.CFGNodes;

            foreach (ParamDeclarationStatement paramDeclaration in funcScope.FuncDeclaration.Params)
            {
                Check(paramDeclaration, funcScope.CFGraph.Root, funcScope);
            }

            foreach (CFGNode node in nodes)
            {
                CheckNode(node, funcScope, errorLogger);
            }
        }

        private void CheckNode(CFGNode node, FuncScope funcScope, ErrorLogger errorLogger)
        {
            foreach (Statement statement in node.Statements)
            {
                try
                {
                    Check(statement, node, funcScope);
                }
                catch(TypingException typingException)
                {
                    errorLogger.Log(typingException);
                }
            }
        }

        private void Check(Statement statement, CFGNode cfgNode, FuncScope funcScope)
        {
            Scope scope = cfgNode.Scope;

            switch (statement)
            {
                case VarDeclarationStatement varDeclaration:
                    if (!scope.IsTypeDefined(varDeclaration.TypeInit))
                        throw new TypeNotDefinedException(varDeclaration.TypeInit);

                    if (scope.IsDeclared(varDeclaration.Name.Lexeme))
                        throw new VarAlreadyDeclaredException(varDeclaration);

                    scope.Declare(varDeclaration);

                    varDeclaration.Assignment?.Accept(_expressionChecker, new ExpressionChecker.ExpressionCheckContext(cfgNode));
                    break;
                case ParamDeclarationStatement paramDeclaration:
                    if (!scope.IsTypeDefined(paramDeclaration.TypeInit))
                        throw new TypeNotDefinedException(paramDeclaration.TypeInit);

                    if (scope.IsDeclared(paramDeclaration.Name.Lexeme))
                        throw new VarAlreadyDeclaredException(paramDeclaration);

                    scope.Declare(paramDeclaration);
                    cfgNode.AddAssignment(scope.GetVar(paramDeclaration.Name.Lexeme), paramDeclaration.TypeInit);
                    break;
                case ExpressionStatement expressionStatement:
                    expressionStatement.Expression.Accept(_expressionChecker, new ExpressionChecker.ExpressionCheckContext(cfgNode));
                    break;
                case ReturnStatement returnStatement:
                    CobraType returnStatementType = returnStatement.Value.Accept(_expressionChecker, new ExpressionChecker.ExpressionCheckContext(cfgNode)).Type;
                    if (!returnStatementType.CanCastTo(scope.GetReturnType()))
                        throw new InvalidReturnTypeException(returnStatement.Value, scope.GetReturnType());
                    break;
                case ImportStatement importStatement:
                    CobraType importType = importStatement.Import.Accept(_expressionChecker, new ExpressionChecker.ExpressionCheckContext(cfgNode)).Type;
                    if (!(importType is NamespaceType))
                        throw new InvalidImportException(importStatement);

                    scope.Declare(importStatement, importType);
                    break;
                case GuardStatement guardStatement:
                    ExpressionType guardCondition = guardStatement.Condition.Accept(_expressionChecker, new ExpressionChecker.ExpressionCheckContext(cfgNode));
                    if (!guardCondition.Type.CanCastTo(DotNetCobraType.Bool))
                        throw new InvalidConditionTypeException(guardStatement.Condition);

                    CFGNode guardElseNode = cfgNode.Next.First();
                    CFGNode guardPassNode = cfgNode.Next.Skip(1).First();

                    foreach (TypeAssertion typeAssertion in guardCondition.TypeAssertions)
                    {
                        // first next is then node
                        guardElseNode.AddAssignment(guardElseNode.Scope.Declare(typeAssertion.Inverted()), typeAssertion.Expression);
                        guardPassNode.AddAssignment(guardPassNode.Scope.Declare(typeAssertion), typeAssertion.Expression);
                    }
                    break;
                case IConditionalExpression conditionalExpression:
                    ExpressionType conditionType = conditionalExpression.Condition.Accept(_expressionChecker, new ExpressionChecker.ExpressionCheckContext(cfgNode));
                    if (!conditionType.Type.CanCastTo(DotNetCobraType.Bool))
                        throw new InvalidConditionTypeException(conditionalExpression.Condition);

                    CFGNode thenNode = cfgNode.Next.First();
                    CFGNode elseNode = conditionalExpression.Else != null ? cfgNode.Next.Skip(1).First() : null;

                    foreach (TypeAssertion typeAssertion in conditionType.TypeAssertions)
                    {
                        // first next is then node
                        thenNode.AddAssignment(thenNode.Scope.Declare(typeAssertion), typeAssertion.Expression);
                        elseNode?.AddAssignment(elseNode?.Scope.Declare(typeAssertion.Inverted()), typeAssertion.Expression);
                    }
                    break;
                case PanicStatement panicStatement:
                    ExpressionType argType = panicStatement.Argument.Accept(_expressionChecker,
                        new ExpressionChecker.ExpressionCheckContext(cfgNode));
                    if(!argType.Type.Equals(DotNetCobraType.Str))
                        throw new InvalidPanicTypeException(panicStatement);
                    break;
                default:
                    throw new NotImplementedException($"Type checking not defined for statement of {statement.GetType()}");
            }
        }
    }
}
