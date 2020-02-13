using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.VisualStyles;
using CobraCompiler.ErrorLogging;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Parse.TypeCheck.Operators;
using CobraCompiler.Parse.TypeCheck.Types;

namespace CobraCompiler.Parse.TypeCheck
{
    class TypeChecker: IExpressionVisitor<CobraType>
    {
        private readonly ErrorLogger _errorLogger;

        private readonly Scope _globalScope;

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

        public void Check(IReadOnlyList<Statement> statements, string moduleName)
        {
            CreateModule(statements, moduleName);
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
                    scope.GetType(funcDeclaration.ReturnType));

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
                if (statement is VarDeclarationStatement varDeclaration)
                {
                    if (!scope.IsTypeDefined(varDeclaration.TypeInit))
                        _errorLogger.Log(new TypeNotDefinedException(varDeclaration.TypeInit.Identifier.First()));

                    if (scope.IsDeclared(varDeclaration.Name.Lexeme))
                        _errorLogger.Log(new VarAlreadyDeclaredException(varDeclaration.Name));

                    scope.Declare(varDeclaration.Name.Lexeme, varDeclaration.TypeInit);
                    varDeclaration.Assignment?.Accept(this);
                }

                if (statement is ParamDeclarationStatement paramDeclaration)
                {
                    if (!scope.IsTypeDefined(paramDeclaration.TypeInit))
                        _errorLogger.Log(new TypeNotDefinedException(paramDeclaration.TypeInit.Identifier.First()));

                    if(scope.IsDeclared(paramDeclaration.Name.Lexeme))
                        _errorLogger.Log(new VarAlreadyDeclaredException(paramDeclaration.Name));

                    scope.Declare(paramDeclaration.Name.Lexeme, paramDeclaration.TypeInit);
                }

                if (statement is ExpressionStatement expressionStatement)
                {
                    expressionStatement.Expression.Accept(this);
                }

                if (statement is ReturnStatement returnStatement)
                {
                    CobraType returnStatementType = returnStatement.Value.Accept(this);
                    if(returnStatementType != scope.GetReturnType())
                        _errorLogger.Log(new InvalidReturnTypeException(returnStatement.Keyword, returnStatementType, scope.GetReturnType()));
                }
            }
        }

        public CobraType Visit(AssignExpression expr)
        {
            if(!CurrentScope.IsDefined(expr.Name.Lexeme))
                _errorLogger.Log(new VarNotDefinedException(expr.Name.Lexeme, expr.Name.Line));

            CobraType varType = CurrentScope.GetVarType(expr.Name.Lexeme);
            CobraType assignType = expr.Value.Accept(this);

            if (!varType.CanImplicitCast(assignType))
                _errorLogger.Log(new InvalidAssignmentException(varType.Identifier, assignType?.Identifier, expr.Name.Line));

            return varType;
        }

        public CobraType Visit(BinaryExpression expr)
        {
            CobraType leftType = expr.Left.Accept(this);
            CobraType rightType = expr.Right.Accept(this);

            if (!CurrentScope.IsOperatorDefined(Operator.GetOperation(expr.Op.Type), leftType, rightType))
            {
                _errorLogger.Log(new OperatorNotDefinedException(expr.Op, leftType, rightType));
                return null;
            }

            IOperator op = CurrentScope.GetOperator(Operator.GetOperation(expr.Op.Type), leftType, rightType);

            return op.ResultType;
        }

        public CobraType Visit(CallExpression expr)
        {
            CobraType calleeType = expr.Callee.Accept(this);
            if (calleeType is CobraGenericInstance generic && generic.Base == DotNetCobraGeneric.FuncType)
                return generic.TypeParams.Last();

            _errorLogger.Log(new InvalidOperationException(expr.Paren.Line));
            return null;
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
                _errorLogger.Log(new OperatorNotDefinedException(expr.Op, operand));

            IOperator op = CurrentScope.GetOperator(Operator.GetOperation(expr.Op.Type), null, operand);

            return op.ResultType;
        }

        public CobraType Visit(GetExpression expr)
        {
            throw new NotImplementedException();
        }

        public CobraType Visit(GroupingExpression expr)
        {
            return expr.Inner.Accept(this);
        }

        public CobraType Visit(VarExpression expr)
        {
            return CurrentScope.GetVarType(expr.Name.Lexeme);
        }
    }


}
