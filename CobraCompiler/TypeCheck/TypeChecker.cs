using System;
using System.Collections.Generic;
using System.Linq;
using CobraCompiler.Compiler;
using CobraCompiler.ErrorLogging;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;
using CobraCompiler.TypeCheck.Exceptions;
using CobraCompiler.TypeCheck.Operators;
using CobraCompiler.TypeCheck.Types;
using InvalidOperationException = CobraCompiler.TypeCheck.Exceptions.InvalidOperationException;

namespace CobraCompiler.TypeCheck
{
    class TypeChecker: IExpressionVisitor<CobraType>
    {
        private readonly ErrorLogger _errorLogger;

        private readonly GlobalScope _globalScope;

        private readonly Queue<Scope> _scopes;
        private Scope CurrentScope => _scopes.Peek();

        public TypeChecker(ErrorLogger errorLogger)
        {
            _errorLogger = errorLogger;
            _scopes = new Queue<Scope>();

            _globalScope = new GlobalScope();
            foreach (DotNetCobraType builtinCobraType in DotNetCobraType.DotNetCobraTypes)
                _globalScope.DefineType(builtinCobraType.Identifier, builtinCobraType);
            foreach (CobraGeneric builtinCobraGeneric in DotNetCobraGeneric.BuiltInCobraGenerics)
                _globalScope.DefineGeneric(builtinCobraGeneric.Identifier, builtinCobraGeneric);
            foreach (DotNetBinaryOperator op in DotNetBinaryOperator.OpCodeDotNetBinaryOperators)
                _globalScope.DefineOperator(op.Operator.Operation, op.Operator.Lhs, op.Operator.Rhs, op);
            foreach (GenericOperator genericOperator in GenericOperator.DotNetGenericOperators)
                _globalScope.DefineOperator(genericOperator);

            _globalScope.Declare("printStr", FuncCobraGeneric.FuncType.CreateGenericInstance(new []{DotNetCobraType.Str, DotNetCobraType.Unit}));
            _globalScope.Declare("printInt", FuncCobraGeneric.FuncType.CreateGenericInstance(new[] { DotNetCobraType.Int, DotNetCobraType.Unit }));
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

            _globalScope.Declare(project.Name, globalNamespace);
        }

        public GlobalScope Check(IEnumerable<ParsedModule> modules)
        {
            List<ModuleScope> moduleScopes = new List<ModuleScope>();

            foreach (ParsedModule module in modules)
            {
                BlockStatement baseBlock = new BlockStatement(module.Statements);
                ModuleScope moduleScope = new ModuleScope(_globalScope, baseBlock, module.FullName);
                
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

            return _globalScope;
        }

        private void CheckScope(Scope scope)
        {
            DefineTypes(scope);
            CheckTypes(scope);
        }

        private void DefineTypes(Scope scope)
        {
            List<Statement> statements = new List<Statement>();
            if(scope.Body is BlockStatement blockBody)
                statements.AddRange(blockBody.Body);
            else if (scope.Body is FuncDeclarationStatement funcDeclarationBody)
                statements.Add(funcDeclarationBody.Body);
            else
                statements.Add(scope.Body);


            foreach (Statement statement in statements)
            {
                DefineWithStatement(scope, statement);
            }
        }

        private void DefineWithStatement(Scope scope, Statement statement)
        {
            if (statement is ClassDeclarationStatement classDeclaration)
            {
                ClassScope classScope = new ClassScope(scope, classDeclaration);

                scope.AddSubScope(classScope);

                _scopes.Enqueue(classScope);
            }

            if (statement is FuncDeclarationStatement funcDeclaration)
            {
                CobraType returnType = funcDeclaration.ReturnType == null
                    ? DotNetCobraType.Unit
                    : scope.GetType(funcDeclaration.ReturnType);

                if (funcDeclaration is InitDeclarationStatement)
                    returnType = CurrentScope.GetType((CurrentScope as ClassScope).ClassDeclaration.Type);

                FuncScope funcScope = new FuncScope(scope, funcDeclaration,
                    funcDeclaration.Params.Select(param => (param.Name.Lexeme, scope.GetType(param.TypeInit))),
                    returnType);

                List<CobraType> typeArgs = funcDeclaration.Params.Select(param => scope.GetType(param.TypeInit)).ToList();
                typeArgs.Add(funcScope.ReturnType);

                CobraGenericInstance funcType = FuncCobraGeneric.FuncType.CreateGenericInstance(typeArgs);

                scope.AddSubScope(funcScope);
                scope.Declare(funcDeclaration.Name.Lexeme, funcType, true);

                _scopes.Enqueue(funcScope);
            }

            if (statement is TypeDeclarationStatement typeDeclaration)
            {
                CobraType type = scope.GetType(typeDeclaration.Type, typeDeclaration.Name.Lexeme);
                scope.DefineType(typeDeclaration.Name.Lexeme, type);
            }

            if (statement is BlockStatement blockStatement)
            {
                Scope blockScope = new Scope(scope, blockStatement);

                scope.AddSubScope(blockScope);
                _scopes.Enqueue(blockScope);
            }

            if (statement is IConditionalExpression conditional)
            {
                DefineWithStatement(scope, conditional.Then);
                DefineWithStatement(scope, conditional.Else);
            }
        }

        private void CheckTypes(Scope scope)
        {
            List<Statement> statements = new List<Statement>();

            if (scope.Body is BlockStatement blockBody)
                statements.AddRange(blockBody.Body);
            else if (scope.Body is FuncDeclarationStatement funcDeclarationBody)
                statements.AddRange(funcDeclarationBody.Params);
            else
                statements.Add(scope.Body);

            foreach (Statement statement in statements)
            {
                Check(statement);
            }


            if (scope is ClassScope classScope)
            {
                CobraType classType = CurrentScope.GetType(classScope.ClassDeclaration.Type);
                HashSet<KeyValuePair<string, CobraType>> definedSymbols = new HashSet<KeyValuePair<string, CobraType>>(classScope.ThisType.Symbols);
                HashSet<KeyValuePair<string, CobraType>> requiredSymbols = new HashSet<KeyValuePair<string, CobraType>>(classType.Symbols);

                if (!definedSymbols.IsSupersetOf(requiredSymbols))
                {
                    List<string> missingElements = new List<string>();

                    requiredSymbols.ExceptWith(definedSymbols);
                    foreach (KeyValuePair<string, CobraType> requiredSymbol in requiredSymbols)
                    {
                        missingElements.Add($"({requiredSymbol.Key}: {requiredSymbol.Value.Identifier})");
                    }
                    _errorLogger.Log(new InvalidTypeImplementationException(classScope.ClassDeclaration.Name.Lexeme, classScope.ClassDeclaration.Type.IdentifierStr, missingElements, classScope.ClassDeclaration.Name.Line)); 
                }
            }
        }

        private void Check(Statement statement)
        {
            try
            {
                switch (statement)
                {
                    case VarDeclarationStatement varDeclaration:
                        if (!CurrentScope.IsTypeDefined(varDeclaration.TypeInit))
                            throw new TypeNotDefinedException(varDeclaration.TypeInit.Identifier.First());

                        if (CurrentScope.IsDeclared(varDeclaration.Name.Lexeme))
                            throw new VarAlreadyDeclaredException(varDeclaration.Name);

                        CurrentScope.Declare(varDeclaration.Name.Lexeme, varDeclaration.TypeInit);
                        varDeclaration.Assignment?.Accept(this);
                        break;
                    case ParamDeclarationStatement paramDeclaration:
                        if (!CurrentScope.IsTypeDefined(paramDeclaration.TypeInit))
                            throw new TypeNotDefinedException(paramDeclaration.TypeInit.Identifier.First());

                        if (CurrentScope.IsDeclared(paramDeclaration.Name.Lexeme))
                            throw new VarAlreadyDeclaredException(paramDeclaration.Name);

                        CurrentScope.Declare(paramDeclaration.Name.Lexeme, paramDeclaration.TypeInit);
                        break;
                    case ExpressionStatement expressionStatement:
                        expressionStatement.Expression.Accept(this);
                        break;
                    case ReturnStatement returnStatement:
                        CobraType returnStatementType = returnStatement.Value.Accept(this);
                        if (!returnStatementType.CanCastTo(CurrentScope.GetReturnType()))
                            throw new InvalidReturnTypeException(returnStatement.Keyword,
                                returnStatementType, CurrentScope.GetReturnType());
                        break;
                    case ImportStatement importStatement:
                        CobraType importType = importStatement.Import.Accept(this);
                        if (!(importType is NamespaceType))
                            throw new InvalidImportException(importType.Identifier, importStatement.Keyword.Line);

                        CurrentScope.Declare(((GetExpression) importStatement.Import).Name.Lexeme, importType);
                        break;
                    //default:
                    //    throw new NotImplementedException($"Type checking not defined for statement of {statement.GetType()}");
                }
            }
            catch (TypingException typingException)
            {
                _errorLogger.Log(typingException);
            }
            
        }

        public static CobraType GetExpressionType(Expression expr, Scope scope)
        {
            ErrorLogger dummyLogger = new ErrorLogger();
            TypeChecker dummyChecker = new TypeChecker(dummyLogger);
            
            dummyChecker._scopes.Enqueue(scope);
            CobraType type = expr.Accept(dummyChecker);

            return dummyLogger.HasErrors ? null : type;
        }

        public CobraType Visit(AssignExpression expr)
        {
            CobraType varType = expr.Target.Accept(this);

            CobraType assignType = expr.Value.Accept(this);

            if (!assignType.CanCastTo(varType))
                throw new InvalidAssignmentException(varType.Identifier, assignType?.Identifier, -1); //TODO: make line number correct

            return varType;
        }

        public CobraType Visit(BinaryExpression expr)
        {
            CobraType leftType = expr.Left.Accept(this);
            CobraType rightType = expr.Right.Accept(this);

            if (!CurrentScope.IsOperatorDefined(Operator.GetOperation(expr.Op.Type), leftType, rightType))
            {
                throw new OperatorNotDefinedException(expr.Op, leftType, rightType);
            }

            IOperator op = CurrentScope.GetOperator(Operator.GetOperation(expr.Op.Type), leftType, rightType);

            return op.ResultType;
        }

        public CobraType Visit(CallExpression expr)
        {
            CobraType calleeType = expr.Callee.Accept(this);
            List<CobraType> paramTypes = expr.Arguments.Select(arg => arg.Accept(this)).ToList();

            if (calleeType.IsCallable(paramTypes))
                return calleeType.CallReturn(paramTypes);

            if (calleeType is FuncGenericInstance func)
            {
                if (func.TypeParams.Count - 1 != expr.Arguments.Count)
                {
                    throw new IncorrectArgumentCountException(expr.Paren, expectedArgs: func.TypeParams.Count - 1, providedArgs: expr.Arguments.Count);
                }

                for (int i = 0; i < func.TypeParams.Count - 1; i++)
                {
                    if (!paramTypes[i].CanCastTo(func.TypeParams[i]))
                    {
                        CobraType test = expr.Arguments[i].Accept(this);
                        throw new InvalidArgumentException(expr.Paren, func.TypeParams[i].Identifier, test.Identifier);
                    }
                }
            }
            
            throw new InvalidOperationException(expr.Paren.Line);
        }

        public CobraType Visit(IndexExpression expr)
        {
            CobraType collectionType = expr.Collection.Accept(this);

            foreach (Expression index in expr.Indicies)
            {
                index.Accept(this);
            }

            IOperator getOperator = CurrentScope.GetOperator(Operation.Get, collectionType, DotNetCobraType.Int);
            
            return getOperator.ResultType;
        }

        public CobraType Visit(ListLiteralExpression expr)
        {
            CobraType elementsCommonType = expr.Elements[0].Accept(this);

            foreach (Expression element in expr.Elements)
            {
                elementsCommonType = elementsCommonType.GetCommonParent(element.Accept(this));
            }

            return DotNetCobraGeneric.ListType.CreateGenericInstance(new[] {elementsCommonType});
        }

        public CobraType Visit(LiteralExpression expr)
        {
            return expr.LiteralType;
        }

        public CobraType Visit(TypeInitExpression expr)
        {
            throw new NotImplementedException();
        }

        public CobraType Visit(UnaryExpression expr)
        {
            CobraType operand = expr.Right.Accept(this);

            if (!CurrentScope.IsOperatorDefined(Operator.GetOperation(expr.Op.Type), null, operand))
                throw new OperatorNotDefinedException(expr.Op, operand);

            IOperator op = CurrentScope.GetOperator(Operator.GetOperation(expr.Op.Type), null, operand);

            return op.ResultType;
        }

        public CobraType Visit(GetExpression expr)
        {
            CobraType objType = expr.Obj.Accept(this);

            if (objType is NamespaceType namespaceType)
            {
                if (namespaceType.HasType(expr.Name.Lexeme))
                    return namespaceType.GetType(expr.Name.Lexeme);

                string resolvedName = namespaceType.ResolveName(expr.Name.Lexeme);
                if(!CurrentScope.IsDefined(resolvedName))
                    throw new VarNotDefinedException(resolvedName, expr.Name.Line);

                return CurrentScope.GetVarType(resolvedName);
            }

            if (!objType.HasSymbol(expr.Name.Lexeme))
                throw new InvalidMemberException(objType.Identifier, expr.Name.Lexeme, expr.Name.Line);

            return objType.GetSymbol(expr.Name.Lexeme);
            throw new NotImplementedException();
        }

        public CobraType Visit(GroupingExpression expr)
        {
            return expr.Inner.Accept(this);
        }

        public CobraType Visit(VarExpression expr)
        {
            if(!CurrentScope.IsDefined(expr.Name.Lexeme))
                throw new VarNotDefinedException(expr.Name.Lexeme, expr.Name.Line);

            return CurrentScope.GetVarType(expr.Name.Lexeme);
        }
    }


}
