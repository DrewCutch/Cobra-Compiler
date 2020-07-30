using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using CobraCompiler.Compiler;
using CobraCompiler.ErrorLogging;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Scanning;
using CobraCompiler.TypeCheck.Exceptions;
using CobraCompiler.TypeCheck.Operators;
using CobraCompiler.TypeCheck.Types;

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
            foreach (DotNetCobraType builtinCobraType in DotNetCobraType.DotNetCobraTypes) { 
                _globalScope.DefineType(builtinCobraType.Identifier, builtinCobraType);

            }
            foreach (CobraGeneric builtinCobraGeneric in DotNetCobraGeneric.BuiltInCobraGenerics)
                _globalScope.DefineType(builtinCobraGeneric.Identifier, builtinCobraGeneric);

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
                DeclareFunc(funcDeclaration, scope);

            if (statement is TypeDeclarationStatement typeDeclaration)
                DeclareType(typeDeclaration, scope);

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

        private void DeclareFunc(FuncDeclarationStatement funcDeclaration, Scope scope)
        {
            if (funcDeclaration.TypeArguments.Count > 0)
                scope = PushGenericScope(funcDeclaration, funcDeclaration.TypeArguments, scope);

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

            // Pop the temporary generic scope
            if (funcDeclaration.TypeArguments.Count > 0)
                scope = scope.Parent;

            scope.Declare(funcDeclaration.Name.Lexeme, funcType, true);

            _scopes.Enqueue(funcScope);
        }

        private void DeclareType(TypeDeclarationStatement typeDeclaration, Scope scope)
        {
            CobraType newType;
            if (typeDeclaration.TypeArguments.Count > 0)
            {
                scope = PushGenericScope(typeDeclaration, typeDeclaration.TypeArguments, scope);

                List<GenericTypeParamPlaceholder> typeParams = new List<GenericTypeParamPlaceholder>();
                int i = 0;
                foreach (Token typeArgument in typeDeclaration.TypeArguments)
                {
                    typeParams.Add(new GenericTypeParamPlaceholder(typeArgument.Lexeme, i));
                    i++;
                }

                CobraGeneric generic = new CobraGeneric(typeDeclaration.Name.Lexeme, typeParams);

                newType = generic;
            }
            else
            {
                newType = new CobraType(typeDeclaration.Name.Lexeme);
            }

            CobraType type = scope.GetType(typeDeclaration.Type, newType);

            // Pop the temporary generic scope
            if (typeDeclaration.TypeArguments.Count > 0)
                scope = scope.Parent;

            scope.DefineType(typeDeclaration.Name.Lexeme, type);
        }

        private Scope PushGenericScope(Statement body, IEnumerable<Token> typeArguments, Scope scope)
        {
            Scope genericScope = new Scope(CurrentScope, body);
            int i = 0;
            foreach (Token typeArg in typeArguments)
            {
                genericScope.DefineType(typeArg.Lexeme, new GenericTypeParamPlaceholder(typeArg.Lexeme, i));
                i++;
            }

            scope.AddSubScope(genericScope);

            return genericScope;
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
                    _errorLogger.Log(new InvalidTypeImplementationException(classScope.ClassDeclaration, classScope.ClassDeclaration.Type, missingElements)); 
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
                            throw new TypeNotDefinedException(varDeclaration.TypeInit);

                        if (CurrentScope.IsDeclared(varDeclaration.Name.Lexeme))
                            throw new VarAlreadyDeclaredException(varDeclaration);

                        CurrentScope.Declare(varDeclaration.Name.Lexeme, varDeclaration.TypeInit);
                        varDeclaration.Assignment?.Accept(this);
                        break;
                    case ParamDeclarationStatement paramDeclaration:
                        if (!CurrentScope.IsTypeDefined(paramDeclaration.TypeInit))
                            throw new TypeNotDefinedException(paramDeclaration.TypeInit);

                        if (CurrentScope.IsDeclared(paramDeclaration.Name.Lexeme))
                            throw new VarAlreadyDeclaredException(paramDeclaration);

                        CurrentScope.Declare(paramDeclaration.Name.Lexeme, paramDeclaration.TypeInit);
                        break;
                    case ExpressionStatement expressionStatement:
                        expressionStatement.Expression.Accept(this);
                        break;
                    case ReturnStatement returnStatement:
                        CobraType returnStatementType = returnStatement.Value.Accept(this);
                        if (!returnStatementType.CanCastTo(CurrentScope.GetReturnType()))
                            throw new InvalidReturnTypeException(returnStatement.Value, CurrentScope.GetReturnType());
                        break;
                    case ImportStatement importStatement:
                        CobraType importType = importStatement.Import.Accept(this);
                        if (!(importType is NamespaceType))
                            throw new InvalidImportException(importStatement);

                        CurrentScope.Declare(((GetExpression) importStatement.Import).Name.Lexeme, importType);
                        break;
                    case IConditionalExpression conditionalExpression:
                        CobraType conditionType = conditionalExpression.Condition.Accept(this);
                        if (!conditionType.CanCastTo(DotNetCobraType.Bool))
                            throw new InvalidConditionTypeException(conditionalExpression.Condition);
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
                throw new InvalidAssignmentException(expr);

            expr.Type = varType;

            return varType;
        }

        public CobraType Visit(BinaryExpression expr)
        {
            CobraType leftType = expr.Left.Accept(this);
            CobraType rightType = expr.Right.Accept(this);

            if (!CurrentScope.IsOperatorDefined(Operator.GetOperation(expr.Op.Type), leftType, rightType))
            {
                throw new OperatorNotDefinedException(expr);
            }

            IOperator op = CurrentScope.GetOperator(Operator.GetOperation(expr.Op.Type), leftType, rightType);

            expr.Type = op.ResultType;

            return op.ResultType;
        }

        public CobraType Visit(CallExpression expr)
        {
            CobraType calleeType = expr.Callee.Accept(this);
            List<CobraType> paramTypes = expr.Arguments.Select(arg => arg.Accept(this)).ToList();

            if (calleeType.IsCallable(paramTypes))
            {
                expr.Type = calleeType.CallReturn(paramTypes);
                return calleeType.CallReturn(paramTypes);
            }

            if (calleeType is FuncGenericInstance func)
            {
                if (func.TypeParams.Count - 1 != expr.Arguments.Count)
                {
                    throw new IncorrectArgumentCountException(expr, func.TypeParams.Count - 1);
                }

                for (int i = 0; i < func.TypeParams.Count - 1; i++)
                {
                    if (!paramTypes[i].CanCastTo(func.OrderedTypeParams[i]))
                    {
                        CobraType test = expr.Arguments[i].Accept(this);
                        throw new InvalidArgumentException(expr.Arguments[i], func.OrderedTypeParams[i].Identifier);
                    }
                }
            }
            
            throw new InvalidCallException(expr);
        }

        public CobraType Visit(IndexExpression expr)
        {
            CobraType collectionType = expr.Collection.Accept(this);

            List<CobraType> typeParams = new List<CobraType>();
            foreach (Expression expression in expr.Indicies)
            {
                CobraType exprType = expression.Accept(this);
                if(exprType is CobraTypeCobraType typeType && typeType.CobraType is CobraType simpleType)
                    typeParams.Add(simpleType);
                else
                    throw new InvalidGenericArgumentException(expression);
            }
            
            if (collectionType is CobraGenericInstance genericInstance)
            {
                expr.Type = genericInstance.ReplacePlaceholders(typeParams);
                return expr.Type;
            }

            if (collectionType is CobraTypeCobraType metaType && metaType.CobraType is CobraGeneric generic)
            {
                expr.Type = generic.CreateGenericInstance(typeParams);
                return new CobraTypeCobraType(expr.Type);
            }

            throw new NotImplementedException();

            /*
            CobraType collectionType = expr.Collection.Accept(this);
            List<CobraType> indexTypes = expr.Indicies.Select(index => index.Accept(this)).ToList();


            IOperator getOperator = CurrentScope.GetOperator(Operation.Get, collectionType, DotNetCobraType.Int);

            expr.Type = getOperator.ResultType;

            return getOperator.ResultType;
            */
        }

        public CobraType Visit(ListLiteralExpression expr)
        {
            CobraType elementsCommonType = expr.Elements[0].Accept(this);

            foreach (Expression element in expr.Elements)
            {
                elementsCommonType = elementsCommonType.GetCommonParent(element.Accept(this));
            }

            CobraType listType = DotNetCobraGeneric.ListType.CreateGenericInstance(new[] { elementsCommonType });

            expr.Type = listType;

            return listType;
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
                throw new OperatorNotDefinedException(expr);

            IOperator op = CurrentScope.GetOperator(Operator.GetOperation(expr.Op.Type), null, operand);

            expr.Type = op.ResultType;

            return op.ResultType;
        }

        public CobraType Visit(GetExpression expr)
        {
            CobraType objType = expr.Obj.Accept(this);

            if (objType is NamespaceType namespaceType)
            {
                if (namespaceType.HasType(expr.Name.Lexeme))
                {
                    expr.Type = namespaceType.GetType(expr.Name.Lexeme);
                    return namespaceType.GetType(expr.Name.Lexeme);
                }

                string resolvedName = namespaceType.ResolveName(expr.Name.Lexeme);
                if(!CurrentScope.IsDefined(resolvedName))
                    throw new VarNotDefinedException(expr, resolvedName);

                CobraType varType = CurrentScope.GetVarType(resolvedName);
                
                expr.Type = varType;
                return varType;
            }

            if (!objType.HasSymbol(expr.Name.Lexeme))
                throw new InvalidMemberException(expr);


            CobraType symbolType = objType.GetSymbol(expr.Name.Lexeme);

            expr.Type = symbolType;
            return symbolType;
        }

        public CobraType Visit(GroupingExpression expr)
        {
            return expr.Inner.Accept(this);
        }

        public CobraType Visit(VarExpression expr)
        {
            if(!CurrentScope.IsDefined(expr.Name.Lexeme))
                throw new VarNotDefinedException(expr);

            CobraType varType = CurrentScope.GetVarType(expr.Name.Lexeme);
            expr.Type = varType;

            return varType;
        }
    }


}
