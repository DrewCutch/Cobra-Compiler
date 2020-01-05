using System;
using System.Collections.Generic;
using System.Linq;
using CobraCompiler.ErrorLogging;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Parse.TypeCheck.Operators;

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
            foreach (CobraGeneric builtinCobraGeneric in CobraGeneric.BuiltInCobraGenerics)
                _globalScope.DefineGeneric(builtinCobraGeneric.Identifier, builtinCobraGeneric.NumberOfParams);
            foreach (DotNetBinaryOperator op in DotNetBinaryOperator.BuiltinDotNetBinaryOperators)
                _globalScope.DefineOperator(op.OperatorToken, op.Lhs, op.Rhs, op);

            _globalScope.Declare("printStr", _globalScope.GetGenericInstance("func", new []{DotNetCobraType.Str, DotNetCobraType.Null}));
            _globalScope.Declare("printInt", _globalScope.GetGenericInstance("func", new[] { DotNetCobraType.Int, DotNetCobraType.Null }));
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
                if (statement is FuncDeclarationStatement funcDeclaration)
                {
                    FuncScope funcScope = new FuncScope(scope, funcDeclaration,
                        funcDeclaration.Params.Select(param => (param.Name.Lexeme, scope.GetType(param.TypeInit.IdentifierStr))), 
                        scope.GetType(funcDeclaration.ReturnType?.Lexeme));

                    List<CobraType> typeArgs = funcDeclaration.Params.Select(param => scope.GetType(param.TypeInit.IdentifierStr)).ToList();
                    typeArgs.Add(funcScope.ReturnType);

                    CobraGenericInstance funcType = scope.GetGenericInstance("func", typeArgs);

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
                    if (!scope.IsTypeDefined(varDeclaration.TypeInit.IdentifierStr))
                        _errorLogger.Log(new TypeNotDefinedException(varDeclaration.TypeInit.Identifier.First()));

                    if (scope.IsDeclared(varDeclaration.Name.Lexeme))
                        _errorLogger.Log(new VarAlreadyDeclaredException(varDeclaration.Name));

                    scope.Declare(varDeclaration.Name.Lexeme, varDeclaration.TypeInit.IdentifierStr);
                    varDeclaration.Assignment?.Accept(this);
                }

                if (statement is ParamDeclarationStatement paramDeclaration)
                {
                    if (!scope.IsTypeDefined(paramDeclaration.TypeInit.IdentifierStr))
                        _errorLogger.Log(new TypeNotDefinedException(paramDeclaration.TypeInit.Identifier.First()));

                    if(scope.IsDeclared(paramDeclaration.Name.Lexeme))
                        _errorLogger.Log(new VarAlreadyDeclaredException(paramDeclaration.Name));

                    scope.Declare(paramDeclaration.Name.Lexeme, paramDeclaration.TypeInit.IdentifierStr);
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

            if (!CurrentScope.IsOperatorDefined(expr.Op.Type, leftType, rightType))
            {
                _errorLogger.Log(new OperatorNotDefinedException(expr.Op, leftType, rightType));
                return null;
            }

            Operator op = CurrentScope.GetOperator(expr.Op.Type, leftType, rightType);

            return op.ResultType;
        }

        public CobraType Visit(CallExpression expr)
        {
            CobraType calleeType = expr.Callee.Accept(this);
            if (calleeType is CobraGenericInstance generic && generic.Identifier == "func")
                return generic.TypeParams.Last();

            _errorLogger.Log(new InvalidOperationException(expr.Paren.Line));
            return null;
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

            if (!CurrentScope.IsOperatorDefined(expr.Op.Type, null, operand))
                _errorLogger.Log(new OperatorNotDefinedException(expr.Op, operand));

            Operator op = CurrentScope.GetOperator(expr.Op.Type, null, operand);

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
