using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Assemble;
using CobraCompiler.ErrorLogging;
using CobraCompiler.Parse.CFG;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Scanning;
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

            CobraGenericInstance funcType = DotNetCobraGeneric.FuncType.CreateGenericInstance(typeArgs);

            scope.AddSubScope(funcScope);

            scope.Declare(funcDeclaration, funcType);

            // Pop the temporary generic scope
            if (funcDeclaration.TypeArguments.Count > 0)
                scope = scope.Parent;

            BuildCFG(funcScope, funcScope.CFGRoot);

            return funcScope;
        }

        public static Scope PushGenericScope(Statement body, IEnumerable<Token> typeArguments, Scope scope)
        {
            Scope genericScope = new GenericScope(scope, body);
            int i = 0;
            foreach (Token typeArg in typeArguments)
            {
                genericScope.DefineType(typeArg.Lexeme, new GenericTypeParamPlaceholder(typeArg.Lexeme, i));
                i++;
            }

            scope.AddSubScope(genericScope);

            return genericScope;
        }

        private static CFGNode BuildCFG(Scope scope, CFGNode root)
        {
            CFGNode previous = root;

            foreach (Statement statement in scope.Body)
            {
                if (statement is BlockStatement block)
                {
                    Scope blockScope = new Scope(scope, block.Body.ToArray());
                    scope.AddSubScope(blockScope);

                    previous = BuildCFG(blockScope, previous);
                }
                else
                {
                    previous.AddStatement(statement);
                }

                if (statement is IConditionalExpression conditional)
                {
                    CFGNode conditionalEnd = new CFGNode(scope);

                    Scope thenScope = new Scope(scope, conditional.Then);
                    scope.AddSubScope(thenScope);

                    CFGNode thenNode = new CFGNode(thenScope);
                    CFGNode.Link(previous, thenNode);

                    CFGNode thenEnd = BuildCFG(thenScope, thenNode);
                    CFGNode.Link(thenEnd, conditionalEnd);

                    if (conditional.Else != null)
                    {
                        Scope elseScope = new Scope(scope, conditional.Else);
                        scope.AddSubScope(elseScope);

                        CFGNode elseNode = new CFGNode(elseScope);
                        CFGNode.Link(previous, elseNode);

                        CFGNode elseEnd = BuildCFG(elseScope, elseNode);
                        CFGNode.Link(elseEnd, conditionalEnd);
                    }
                    else
                    {
                        CFGNode.Link(previous, conditionalEnd);
                    }

                    previous = conditionalEnd;
                }
            }

            return previous;
        }

        public void CheckFunc(FuncScope funcScope)
        {
            List<CFGNode> nodes = CFGNode.LinearNodes(funcScope.CFGRoot);

            foreach (ParamDeclarationStatement paramDeclaration in funcScope.FuncDeclaration.Params)
            {
                Check(paramDeclaration, funcScope.CFGRoot, funcScope);
            }

            foreach (CFGNode node in nodes)
            {
                CheckNode(node, funcScope);
            }
        }

        private void CheckNode(CFGNode node, FuncScope funcScope)
        {
            foreach (Statement statement in node.Statements)
            {
                Check(statement, node, funcScope);
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
                    varDeclaration.Assignment?.Accept(_expressionChecker, cfgNode);
                    break;
                case ParamDeclarationStatement paramDeclaration:
                    if (!scope.IsTypeDefined(paramDeclaration.TypeInit))
                        throw new TypeNotDefinedException(paramDeclaration.TypeInit);

                    if (scope.IsDeclared(paramDeclaration.Name.Lexeme))
                        throw new VarAlreadyDeclaredException(paramDeclaration);

                    scope.Declare(paramDeclaration);
                    break;
                case ExpressionStatement expressionStatement:
                    expressionStatement.Expression.Accept(_expressionChecker, cfgNode);
                    break;
                case ReturnStatement returnStatement:
                    CobraType returnStatementType = returnStatement.Value.Accept(_expressionChecker, cfgNode);
                    if (!returnStatementType.CanCastTo(scope.GetReturnType()))
                        throw new InvalidReturnTypeException(returnStatement.Value, scope.GetReturnType());

                    cfgNode.SetNext(funcScope.CFGTerminal);
                    break;
                case ImportStatement importStatement:
                    CobraType importType = importStatement.Import.Accept(_expressionChecker, cfgNode);
                    if (!(importType is NamespaceType))
                        throw new InvalidImportException(importStatement);

                    scope.Declare(importStatement, importType);
                    break;
                case IConditionalExpression conditionalExpression:
                    CobraType conditionType = conditionalExpression.Condition.Accept(_expressionChecker, cfgNode);
                    if (!conditionType.CanCastTo(DotNetCobraType.Bool))
                        throw new InvalidConditionTypeException(conditionalExpression.Condition);
                    break;
                default:
                    throw new NotImplementedException($"Type checking not defined for statement of {statement.GetType()}");
            }
        }
    }
}
