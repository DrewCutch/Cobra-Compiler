using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using CobraCompiler.Compiler;
using CobraCompiler.ErrorLogging;
using CobraCompiler.Parse.CFG;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Scanning;
using CobraCompiler.SupportedProject;
using CobraCompiler.TypeCheck.CFG;
using CobraCompiler.TypeCheck.Definers;
using CobraCompiler.TypeCheck.Exceptions;
using CobraCompiler.TypeCheck.Operators;
using CobraCompiler.TypeCheck.Symbols;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.TypeCheck
{
    class TypeChecker
    {
        private readonly ErrorLogger _errorLogger;

        private readonly GlobalScope _globalScope;

        private readonly FuncChecker _funcChecker;
        private readonly ExpressionChecker _expressionChecker;

        private readonly Queue<Scope> _scopes;
        private Scope CurrentScope => _scopes.Peek();

        public TypeChecker(ErrorLogger errorLogger)
        {
            _errorLogger = errorLogger;
            _scopes = new Queue<Scope>();
            _funcChecker = new FuncChecker();
            _expressionChecker = new ExpressionChecker();

            _globalScope = new GlobalScope();
            foreach (DotNetCobraType builtinCobraType in DotNetCobraType.DotNetCobraTypes)
            {
                _globalScope.DefineType(builtinCobraType.Identifier, builtinCobraType);

            }

            foreach (CobraGeneric builtinCobraGeneric in DotNetCobraGeneric.BuiltInCobraGenerics)
                _globalScope.DefineType(builtinCobraGeneric.Identifier, builtinCobraGeneric);

            foreach (DotNetBinaryOperator op in DotNetBinaryOperator.OpCodeDotNetBinaryOperators)
                _globalScope.DefineOperator(op.Operator.Operation, op.Operator.Lhs, op.Operator.Rhs, op);
            foreach (GenericOperator genericOperator in GenericOperator.DotNetGenericOperators)
                _globalScope.DefineOperator(genericOperator);

            _globalScope.Declare(null, "printStr",
                FuncCobraGeneric.FuncType.CreateGenericInstance(new[] {DotNetCobraType.Str, DotNetCobraType.Unit}),
                SymbolKind.Global, Mutability.AssignOnce);
            _globalScope.Declare(null, "printInt",
                DotNetCobraGeneric.FuncType.CreateGenericInstance(new[] {DotNetCobraType.Int, DotNetCobraType.Unit}),
                SymbolKind.Global, Mutability.AssignOnce);
        }

        public void DefineNamespaces(Project project)
        {
            Stack<Compiler.System> systems = new Stack<Compiler.System>();
            NamespaceType globalNamespace = new NamespaceType(project.Name);

            foreach (Compiler.System system in project.Systems)
            {

                NamespaceType systemNamespace = globalNamespace.AddSubNameSpace(system.Name);

                foreach (Module module in system.Modules)
                {
                    systemNamespace.AddSubNameSpace(module.Name);
                }
            }

            _globalScope.Declare(null, project.Name, globalNamespace, SymbolKind.Global, Mutability.CompileTimeConstant);
        }

        public GlobalScope Check(IEnumerable<ParsedModule> modules)
        {
            List<ModuleScope> moduleScopes = new List<ModuleScope>();

            foreach (ParsedModule module in modules)
            {
                ModuleScope moduleScope = new ModuleScope(_globalScope, module.Statements.ToArray(), module.FullName);

                _globalScope.AddSubScope(moduleScope);
                moduleScopes.Add(moduleScope);
            }

            foreach (ModuleScope scope in moduleScopes)
            {
                _scopes.Enqueue(scope);
            }

            while (_scopes.Count > 0)
            {
                CheckScope(CurrentScope);
                _scopes.Dequeue();
            }

            CheckReturns(_globalScope);

            return _globalScope;
        }

        private void CheckReturns(Scope scope)
        {
            if (scope is FuncScope funcScope && !(funcScope.FuncDeclaration is InitDeclarationStatement))
            {
                if (!funcScope.Returns && funcScope.ReturnType != DotNetCobraType.Unit)
                    _errorLogger.Log(new MissingReturnException(funcScope.FuncDeclaration.Name, funcScope.ReturnType));
            }

            foreach (Scope subScope in scope.SubScopes)
            {
                CheckReturns(subScope);
            }
        }

        private void CheckScope(Scope scope)
        {
            try
            {
                List<IDefine> typeDefiners = DeclareTypes(scope).ToList();
                typeDefiners.ForEach(definer => definer.Define());

                DefineTypes(scope);
                CheckTypes(scope);
            }
            catch (TypingException typingException)
            {
                _errorLogger.Log(typingException);
            }
        }

        private IEnumerable<IDefine> DeclareTypes(Scope scope)
        {
            foreach (Statement statement in scope.Body)
            {
                if (!(statement is TypeDeclarationStatement typeDeclaration))
                    continue;

                yield return new TypeDefiner(typeDeclaration, scope);
            }
        }

        private void DefineTypes(Scope scope)
        {
            foreach (Statement statement in scope.Body)
                DefineWithStatement(scope, statement);
        }

        private void DefineWithStatement(Scope scope, Statement statement)
        {
            if (statement is ClassDeclarationStatement classDeclaration)
                DefineClass(classDeclaration, scope);

            if (statement is FuncDeclarationStatement funcDeclaration)
                DefineFunc(funcDeclaration, scope);

            if (statement is BlockStatement blockStatement)
            {
                Scope blockScope = new Scope(scope, blockStatement.Body.ToArray());

                scope.AddSubScope(blockScope);
                _scopes.Enqueue(blockScope);
            }

            if (statement is IConditionalExpression conditional)
            {
                Scope thenScope = new Scope(scope, conditional.Then);
                scope.AddSubScope(thenScope);
                _scopes.Enqueue(thenScope);

                if (conditional.Else != null)
                {
                    Scope elseScope = new Scope(scope, conditional.Else);
                    scope.AddSubScope(elseScope);
                    _scopes.Enqueue(elseScope);
                }
            }
        }

        private void DefineClass(ClassDeclarationStatement classDeclaration, Scope scope)
        {
            if (classDeclaration.TypeArguments.Count > 0)
                scope = PushGenericScope(classDeclaration, classDeclaration.TypeArguments, scope);

            ClassScope classScope = new ClassScope(scope, classDeclaration);

            scope.AddSubScope(classScope);

            if (classDeclaration.TypeArguments.Count > 0)
                scope = scope.Parent;

            _scopes.Enqueue(classScope);
        }

        private void DefineFunc(FuncDeclarationStatement funcDeclaration, Scope scope)
        {
            FuncScope funcScope = _funcChecker.DefineFunc(funcDeclaration, scope);
            _scopes.Enqueue(funcScope);
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

        private void CheckAssignVals(ClassScope classScope, FuncScope funcScope)
        {
            foreach (Symbol symbol in classScope.ThisType.Symbols.Values)
            {
                if (symbol.Mutability != Mutability.AssignOnce)
                        continue;

                if (!funcScope.CFGraph.Terminal.FulfilledByAncestors(ControlFlowCheck.IsAssigned(symbol)))
                {
                    throw new IncompleteMemberAssignmentException(symbol, funcScope.FuncDeclaration);
                }
            }
        }

        private void CheckTypes(Scope scope)
        {
            if (scope is FuncScope funcScope)
            {
                _funcChecker.CheckFunc(funcScope, _errorLogger);

                if (funcScope.IsInit)
                    CheckAssignVals(funcScope.Parent as ClassScope, funcScope);
            }
            else if(scope is ModuleScope || scope is ClassScope)
            {
                foreach (Statement statement in scope.Body)
                {
                    Check(statement);
                }
            }

            if (scope is ClassScope classScope)
            {

                CobraType classType = CurrentScope.GetType(classScope.ClassDeclaration.Type);
                HashSet<KeyValuePair<string, CobraType>> definedSymbols =
                    new HashSet<KeyValuePair<string, CobraType>>(classScope.ThisType.Symbols
                        .Select(symbol => new KeyValuePair<string, CobraType>(symbol.Key, symbol.Value.Type)));

                HashSet<KeyValuePair<string, CobraType>> requiredSymbols =
                    new HashSet<KeyValuePair<string, CobraType>>(classType.Symbols
                        .Select(symbol => new KeyValuePair<string, CobraType>(symbol.Key, symbol.Value.Type)));

                if (!definedSymbols.IsSupersetOf(requiredSymbols))
                {
                    List<string> missingElements = new List<string>();

                    requiredSymbols.ExceptWith(definedSymbols);
                    foreach (KeyValuePair<string, CobraType> requiredSymbol in requiredSymbols)
                    {
                        missingElements.Add($"({requiredSymbol.Key}: {requiredSymbol.Value.Identifier})");
                    }

                    _errorLogger.Log(new InvalidTypeImplementationException(classScope.ClassDeclaration,
                        classScope.ClassDeclaration.Type, missingElements));
                }
            }
        }

        private void Check(Statement statement)
        {
            CFGNode moduleNode = CFGNode.CreateDummyNode(CurrentScope);

            switch (statement)
            {
                case VarDeclarationStatement varDeclaration:
                    if (!CurrentScope.IsTypeDefined(varDeclaration.TypeInit))
                        throw new TypeNotDefinedException(varDeclaration.TypeInit);

                    if (CurrentScope.IsDeclared(varDeclaration.Name.Lexeme))
                        throw new VarAlreadyDeclaredException(varDeclaration);

                    CurrentScope.Declare(varDeclaration);
                    varDeclaration.Assignment?.Accept(_expressionChecker, new ExpressionChecker.ExpressionCheckContext(moduleNode));
                    break;
                case ParamDeclarationStatement paramDeclaration:
                    if (!CurrentScope.IsTypeDefined(paramDeclaration.TypeInit))
                        throw new TypeNotDefinedException(paramDeclaration.TypeInit);

                    if (CurrentScope.IsDeclared(paramDeclaration.Name.Lexeme))
                        throw new VarAlreadyDeclaredException(paramDeclaration);

                    CurrentScope.Declare(paramDeclaration);
                    break;
                case ExpressionStatement expressionStatement:
                    expressionStatement.Expression.Accept(_expressionChecker, new ExpressionChecker.ExpressionCheckContext(moduleNode));
                    break;
                case ReturnStatement returnStatement:
                    CobraType returnStatementType = returnStatement.Value.Accept(_expressionChecker, new ExpressionChecker.ExpressionCheckContext(moduleNode)).Type;
                    if (!returnStatementType.CanCastTo(CurrentScope.GetReturnType()))
                        throw new InvalidReturnTypeException(returnStatement.Value, CurrentScope.GetReturnType());
                    break;
                case ImportStatement importStatement:
                    CobraType importType = importStatement.Import.Accept(_expressionChecker, new ExpressionChecker.ExpressionCheckContext(moduleNode)).Type;
                    if (!(importType is NamespaceType))
                        throw new InvalidImportException(importStatement);

                    CurrentScope.Declare(importStatement, importType);
                    break;
                case IConditionalExpression conditionalExpression:
                    CobraType conditionType = conditionalExpression.Condition.Accept(_expressionChecker, new ExpressionChecker.ExpressionCheckContext(moduleNode)).Type;
                    if (!conditionType.CanCastTo(DotNetCobraType.Bool))
                        throw new InvalidConditionTypeException(conditionalExpression.Condition);
                    break;
                //default:
                    //throw new NotImplementedException($"Type checking not defined for statement of {statement.GetType()}");
            }
        }

        /*
        public static CobraType GetExpressionType(Expression expr, Scope scope)
        {
            ErrorLogger dummyLogger = new ErrorLogger();
            TypeChecker dummyChecker = new TypeChecker(dummyLogger);
            
            dummyChecker._scopes.Enqueue(scope);
            CobraType type = expr.Accept(dummyChecker);

            return dummyLogger.HasErrors ? null : type;
        }
        */
    }
}
