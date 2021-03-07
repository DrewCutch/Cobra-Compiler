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

            CFGNode lastNode = BuildCFG(funcScope, funcScope.CFGraph.Root); 
            lastNode.Link(funcScope.CFGraph.Terminal);

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

                    CFGNode conditionalEnd = thenEnd.CreateNext(scope);
                    if(elseEnd != null)
                        elseEnd.Link(conditionalEnd);
                    else
                        previous.Link(conditionalEnd);

                    previous = conditionalEnd;
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

                    cfgNode.SetNext(funcScope.CFGraph.Terminal);
                    break;
                case ImportStatement importStatement:
                    CobraType importType = importStatement.Import.Accept(_expressionChecker, new ExpressionChecker.ExpressionCheckContext(cfgNode)).Type;
                    if (!(importType is NamespaceType))
                        throw new InvalidImportException(importStatement);

                    scope.Declare(importStatement, importType);
                    break;
                case IConditionalExpression conditionalExpression:
                    CobraType conditionType = conditionalExpression.Condition.Accept(_expressionChecker, new ExpressionChecker.ExpressionCheckContext(cfgNode)).Type;
                    if (!conditionType.CanCastTo(DotNetCobraType.Bool))
                        throw new InvalidConditionTypeException(conditionalExpression.Condition);
                    break;
                default:
                    throw new NotImplementedException($"Type checking not defined for statement of {statement.GetType()}");
            }
        }
    }
}
