using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.VisualStyles;
using CobraCompiler.Compiler;
using CobraCompiler.ErrorLogging;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Parse.TypeCheck.Operators;
using CobraCompiler.Parse.TypeCheck.Types;
using System = CobraCompiler.Compiler.System;

namespace CobraCompiler.Parse.TypeCheck
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

            _globalScope.Declare("printStr", DotNetCobraGeneric.FuncType.CreateGenericInstance(new []{DotNetCobraType.Str, DotNetCobraType.Null}));
            _globalScope.Declare("printInt", DotNetCobraGeneric.FuncType.CreateGenericInstance(new[] { DotNetCobraType.Int, DotNetCobraType.Null }));
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

        public ModuleScope CreateModule(IReadOnlyList<Statement> statements, string moduleName)
        {
            BlockStatement baseBlock = new BlockStatement(statements);
            ModuleScope moduleScope = new ModuleScope(_globalScope, baseBlock, moduleName);

            _scopes.Enqueue(moduleScope);

            while (_scopes.Count > 0)
            {
                CheckScope(CurrentScope);
                _scopes.Dequeue();
            }

            return moduleScope;
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
            if (statement is FuncDeclarationStatement funcDeclaration)
            {
                CobraType returnType = funcDeclaration.ReturnType == null
                    ? DotNetCobraType.Unit
                    : scope.GetType(funcDeclaration.ReturnType);

                FuncScope funcScope = new FuncScope(scope, funcDeclaration,
                    funcDeclaration.Params.Select(param => (param.Name.Lexeme, scope.GetType(param.TypeInit))),
                    returnType);

                List<CobraType> typeArgs = funcDeclaration.Params.Select(param => scope.GetType(param.TypeInit)).ToList();
                typeArgs.Add(funcScope.ReturnType);

                CobraGenericInstance funcType = DotNetCobraGeneric.FuncType.CreateGenericInstance(typeArgs);

                scope.AddSubScope(funcScope);
                scope.Declare(funcDeclaration.Name.Lexeme, funcType);

                _scopes.Enqueue(funcScope);
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
                        if (returnStatementType != CurrentScope.GetReturnType())
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
                        //throw new NotImplementedException($"Type checking not defined for statement of {statement.GetType()}");
                }
            }
            catch (TypingException typingException)
            {
                _errorLogger.Log(typingException);
            }
            
        }

        public CobraType Visit(AssignExpression expr)
        {
            if(!CurrentScope.IsDefined(expr.Name.Lexeme))
                throw new VarNotDefinedException(expr.Name.Lexeme, expr.Name.Line);

            CobraType varType = CurrentScope.GetVarType(expr.Name.Lexeme);
            CobraType assignType = expr.Value.Accept(this);

            if (!varType.CanImplicitCast(assignType))
                throw new InvalidAssignmentException(varType.Identifier, assignType?.Identifier, expr.Name.Line);

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
            if (calleeType is CobraGenericInstance generic && generic.Base == DotNetCobraGeneric.FuncType)
            {
                if (generic.TypeParams.Count - 1 != expr.Arguments.Count)
                {
                    throw new IncorrectArgumentCountException(expr.Paren, expectedArgs: generic.TypeParams.Count - 1, providedArgs: expr.Arguments.Count);
                }

                for (int i = 0; i < generic.TypeParams.Count - 1; i++)
                {
                    if (!generic.TypeParams[i].CanImplicitCast(expr.Arguments[i].Accept(this)))
                    {
                        CobraType test = expr.Arguments[i].Accept(this);
                        throw new InvalidArgumentException(expr.Paren);
                    }
                }
                return generic.TypeParams.Last();
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
                elementsCommonType = element.Accept(this).GetCommonParent(elementsCommonType);
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
