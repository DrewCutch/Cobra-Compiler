﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Assemble.ExpressionAssemblyContexts;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Parse.TypeCheck;
using CobraCompiler.Parse.TypeCheck.Operators;

namespace CobraCompiler.Assemble
{
    class FuncAssembler: IExpressionVisitor<ExpressionAssemblyContext>
    {
        public static readonly MethodAttributes FuncMethodAttributes = MethodAttributes.Public | MethodAttributes.Static;

        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly TypeBuilder _typeBuilder;
        private readonly TypeStore _typeStore;


        private ILGenerator _il;
        private readonly FuncScope _funcScope;
        private LocalManager _localManager;

        private readonly ScopeCrawler _scopeCrawler;
        private Scope CurrentScope => _scopeCrawler.Current;

        private readonly MethodStore _methodStore;

        public FuncAssembler(FuncScope funcScope, TypeStore typeStore, MethodStore methodStore, TypeBuilder typeBuilder, AssemblyBuilder assemblyBuilder)
        {
            _funcScope = funcScope;
            _typeStore = typeStore;
            _methodStore = methodStore;
            _typeBuilder = typeBuilder;
            _assemblyBuilder = assemblyBuilder;
            _scopeCrawler = new ScopeCrawler(funcScope);
        }

        public MethodBuilder AssembleDefinition()
        {
            FuncDeclarationStatement funcDeclaration = _funcScope.FuncDeclaration;

            Type[] paramTypes = _funcScope.Params.Select(param => _typeStore.GetType(param.Item2.Identifier)).ToArray();
            Type returnType = _funcScope.ReturnType != null ? _typeStore.GetType(_funcScope.ReturnType.Identifier) : null;

            MethodBuilder methodBuilder = _typeBuilder.DefineMethod(funcDeclaration.Name.Lexeme, FuncMethodAttributes, returnType, paramTypes);

            if (funcDeclaration.Name.Lexeme == "main")
                _assemblyBuilder.SetEntryPoint(methodBuilder);

            for (int i = 0; i < funcDeclaration.Params.Count; i++)
            {
                ParameterBuilder parameterBuilder = methodBuilder.DefineParameter(i + 1, ParameterAttributes.None, funcDeclaration.Params[i].Name.Lexeme);
            }
            
            _il = methodBuilder.GetILGenerator();
            _localManager = new LocalManager(_funcScope, _il, returnType);

            return methodBuilder;
        }

        public void AssembleBody()
        {
            _scopeCrawler.Reset();

            _scopeCrawler.Advance();
            _scopeCrawler.Advance();

            AssembleStatement(CurrentScope.Body, _typeBuilder);
            _il.Emit(OpCodes.Nop);
        }

        private void AssembleStatement(Statement statement, TypeBuilder typeBuilder)
        {
            switch (statement)
            {
                case BlockStatement block:
                    _scopeCrawler.Advance();
                    foreach (Statement stmt in block.Body)
                        AssembleStatement(stmt, typeBuilder);
                    break;
                case ExpressionStatement exprStmt:
                    exprStmt.Expression.Accept(this);
                    break;
                case VarDeclarationStatement varDeclaration:
                    _localManager.DeclareVar(CurrentScope, varDeclaration.Name.Lexeme, _typeStore.GetType(varDeclaration.TypeInit.IdentifierStr));
                    varDeclaration.Assignment?.Accept(this);
                    break;
                case ReturnStatement returnStatement:
                    returnStatement.Value.Accept(this);
                    ReturnStatement();
                    break;
                case IfStatement ifStatement:
                {
                    ifStatement.Condition.Accept(this);

                    Label elseLabel = _il.DefineLabel();
                    Label endElseLabel = _il.DefineLabel();

                    _il.Emit(OpCodes.Brfalse, elseLabel);
                    AssembleStatement(ifStatement.Then, typeBuilder);
                    _il.Emit(OpCodes.Br, endElseLabel);
                    _il.MarkLabel(elseLabel);
                    if (ifStatement.Else != null)
                    {
                        AssembleStatement(ifStatement.Else, typeBuilder);
                    }
                    _il.MarkLabel(endElseLabel);
                    break;
                }   
                case WhileStatement whileStatement:
                {
                    Label elseLabel = _il.DefineLabel();
                    Label endElseLabel = _il.DefineLabel();
                    Label whileLabel = _il.DefineLabel();
                    Label bodyLabel = _il.DefineLabel();

                    whileStatement.Condition.Accept(this);

                    _il.Emit(OpCodes.Brfalse, elseLabel); // If condition fails the first time go to else
                    _il.Emit(OpCodes.Br, bodyLabel); // Else go to body
                    _il.MarkLabel(whileLabel); // Return here on subsequent loop
                    whileStatement.Condition.Accept(this);
                    _il.Emit(OpCodes.Brfalse, endElseLabel);
                    _il.MarkLabel(bodyLabel); // Beginning of body
                    AssembleStatement(whileStatement.Body, typeBuilder);
                    _il.Emit(OpCodes.Br, whileLabel); // Return to condition
                    _il.MarkLabel(elseLabel);
                    if (whileStatement.Else != null)
                    {
                        AssembleStatement(whileStatement.Else, typeBuilder);
                    }
                    _il.MarkLabel(endElseLabel);
                    break;
                }
                default:
                    throw new NotImplementedException();
            }
        }

        private void ReturnStatement()
        {
            _localManager.StoreVar(CurrentScope, "@ret");
            _localManager.LoadVar(CurrentScope, "@ret");
            _il.Emit(OpCodes.Ret);
        }


        public ExpressionAssemblyContext Visit(AssignExpression expr)
        {
            expr.Value.Accept(this);
            _localManager.StoreVar(_scopeCrawler.Current, expr.Name.Lexeme);

            return new ExpressionAssemblyContext(_funcScope.GetVarType(expr.Name.Lexeme));
        }

        public ExpressionAssemblyContext Visit(BinaryExpression expr)
        {
            CobraType leftType = expr.Left.Accept(this).Type;
            CobraType rightType = expr.Right.Accept(this).Type;

            Operator op = _funcScope.GetOperator(expr.Op.Type, leftType, rightType);
            if (op is DotNetBinaryOperator dotNetBinaryOperator)
            {
                _il.Emit(dotNetBinaryOperator.OpCode);
            }

            return new ExpressionAssemblyContext(op.ResultType);
        }

        public ExpressionAssemblyContext Visit(CallExpression expr)
        {
            ExpressionAssemblyContext calleeContext = expr.Callee.Accept(this);

            foreach (Expression arg in expr.Arguments)
                arg.Accept(this);

            CobraType returnType = null;

            if (calleeContext is MethodExpressionAssemblyContext context)
            {
                MethodInfo method = _methodStore.GetMethodInfo(context);

                _il.Emit(OpCodes.Call, method);
            }

            if (calleeContext.Type is CobraGenericInstance funcInstance)
                returnType = funcInstance.TypeParams.Last();

            return new ExpressionAssemblyContext(returnType);
        }

        public ExpressionAssemblyContext Visit(LiteralExpression expr)
        {
            _localManager.LoadLiteral(expr.Value);

            return new ExpressionAssemblyContext(expr.LiteralType);
        }

        public ExpressionAssemblyContext Visit(TypeInitExpression expr)
        {
            throw new NotImplementedException();
        }

        public ExpressionAssemblyContext Visit(UnaryExpression expr)
        {
            CobraType operandType = expr.Right.Accept(this).Type;

            Operator op = _funcScope.GetOperator(expr.Op.Type, null, operandType);
            if (op is DotNetBinaryOperator dotNetBinaryOperator)
            {
                _il.Emit(dotNetBinaryOperator.OpCode);
            }

            return new ExpressionAssemblyContext(op.ResultType);
        }

        public ExpressionAssemblyContext Visit(GroupingExpression expr)
        {
            return expr.Inner.Accept(this);
        }

        public ExpressionAssemblyContext Visit(VarExpression expr)
        {
            
            if(_methodStore.HasMethodBuilder(expr.Name.Lexeme))
                return new MethodBuilderExpressionAssemblyContext(CurrentScope.GetVarType(expr.Name.Lexeme), expr.Name.Lexeme);
            else
                _localManager.LoadVar(_scopeCrawler.Current, expr.Name.Lexeme);

            return new ExpressionAssemblyContext(CurrentScope.GetVarType(expr.Name.Lexeme));
        }

        public ExpressionAssemblyContext Visit(GetExpression expr)
        {
            throw new NotImplementedException();
        }
    }
}
