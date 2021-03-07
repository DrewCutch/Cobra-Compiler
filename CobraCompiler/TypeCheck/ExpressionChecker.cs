﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.CFG;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.TypeCheck.CFG;
using CobraCompiler.TypeCheck.Exceptions;
using CobraCompiler.TypeCheck.Operators;
using CobraCompiler.TypeCheck.Symbols;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.TypeCheck
{
    class ExpressionChecker : IExpressionVisitorWithContext<ExpressionType, ExpressionChecker.ExpressionCheckContext>
    {
        public class ExpressionCheckContext
        {
            public readonly CFGNode CfgNode;
            public readonly bool IsAssigning;

            public ExpressionCheckContext(CFGNode cfgNode, bool isAssigning = false)
            {
                CfgNode = cfgNode;
                IsAssigning = isAssigning;
            }
        }

        public ExpressionType Visit(AssignExpression expr, ExpressionCheckContext context)
        {
            ExpressionType var = expr.Target.Accept(this, new ExpressionCheckContext(context.CfgNode, isAssigning:true));

            ExpressionType value = expr.Value.Accept(this, context);

            if (!value.Type.CanCastTo(var.Type))
                throw new InvalidAssignmentException(expr);

            if (var.Symbol?.Kind == SymbolKind.Param)
                throw new AssignToParamException(expr, var.Symbol);

            if(var.Mutability != Mutability.Mutable && var.Mutability != Mutability.AssignOnce)
                throw new WriteToReadOnlySymbolException(expr);

            if(var.Mutability == Mutability.AssignOnce && context.CfgNode.FulfilledByAnyAncestors(ControlFlowCheck.IsAssigned(var.Symbol)))
                throw new WriteToReadOnlySymbolException(expr);

            expr.Type = var.Type;

            context.CfgNode.AddAssignment(var.Symbol, expr);

            return new ExpressionType(var.Type, MutabilityUtils.GetResultMutability(var.Mutability, value.Mutability),
                null);
        }

        public ExpressionType Visit(BinaryExpression expr, ExpressionCheckContext context)
        {
            ExpressionType left = expr.Left.Accept(this, context);
            ExpressionType right = expr.Right.Accept(this, context);

            if (!context.CfgNode.Scope.IsOperatorDefined(Operator.GetOperation(expr.Op.Type), left.Type, right.Type))
            {
                throw new OperatorNotDefinedException(expr);
            }

            IOperator op = context.CfgNode.Scope.GetOperator(Operator.GetOperation(expr.Op.Type), left.Type, right.Type);

            expr.Type = op.ResultType;

            return new ExpressionType(op.ResultType,
                MutabilityUtils.GetResultMutability(left.Mutability, right.Mutability), null);
        }

        public ExpressionType Visit(CallExpression expr, ExpressionCheckContext context)
        {
            CobraType calleeType = expr.Callee.Accept(this, context).Type;
            List<CobraType> paramTypes = expr.Arguments.Select(arg => arg.Accept(this, context).Type).ToList();

            if (calleeType.IsCallable(paramTypes))
            {
                expr.Type = calleeType.CallReturn(paramTypes);
                return new ExpressionType(calleeType.CallReturn(paramTypes), Mutability.Result, null);
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
                        throw new InvalidArgumentException(expr.Arguments[i], func.OrderedTypeParams[i].Identifier);
                    }
                }
            }

            throw new InvalidCallException(expr);
        }

        public ExpressionType Visit(IndexExpression expr, ExpressionCheckContext context)
        {
            CobraType collectionType = expr.Collection.Accept(this, context).Type;

            List<CobraType> typeParams = new List<CobraType>();
            foreach (Expression expression in expr.Indicies)
            {
                CobraType exprType = expression.Accept(this, context).Type;
                if (exprType is CobraTypeCobraType typeType && typeType.CobraType is CobraType simpleType)
                    typeParams.Add(simpleType);
                else
                    throw new InvalidGenericArgumentException(expression);
            }

            if (collectionType is CobraGenericInstance genericInstance)
            {
                expr.Type = genericInstance.ReplacePlaceholders(typeParams);
                return new ExpressionType(expr.Type, Mutability.Result, null);
            }

            if (collectionType is CobraTypeCobraType metaType && metaType.CobraType is CobraGeneric generic)
            {
                expr.Type = generic.CreateGenericInstance(typeParams);
                return new ExpressionType(new CobraTypeCobraType(expr.Type), Mutability.CompileTimeConstantResult, null);
            }

            throw new NotImplementedException();

            /*
            CobraType collectionType = expr.Collection.Accept(this);
            List<CobraType> indexTypes = expr.Indicies.Select(index => index.Accept(this)).ToList();


            IOperator getOperator = CurrentcfgNode.Scope.GetOperator(Operation.Get, collectionType, DotNetCobraType.Int);

            expr.Type = getOperator.ResultType;

            return getOperator.ResultType;
            */
        }

        public ExpressionType Visit(ListLiteralExpression expr, ExpressionCheckContext context)
        {
            ExpressionType firstElement = expr.Elements[0].Accept(this, context);
            CobraType elementsCommonType = firstElement.Type;
            Mutability elementsMutability = firstElement.Mutability;

            foreach (Expression element in expr.Elements)
            {
                ExpressionType elementExpressionType = element.Accept(this, context);

                elementsCommonType = elementsCommonType.GetCommonParent(elementExpressionType.Type);
                
                elementsMutability = MutabilityUtils.GetResultMutability(elementsMutability, elementExpressionType.Mutability);
            }

            CobraType listType = DotNetCobraGeneric.ListType.CreateGenericInstance(new[] { elementsCommonType });

            expr.Type = listType;

            return new ExpressionType(listType, elementsMutability, null);
        }

        public ExpressionType Visit(LiteralExpression expr, ExpressionCheckContext context)
        {
            return new ExpressionType(expr.LiteralType, Mutability.CompileTimeConstantResult, null);
        }

        public ExpressionType Visit(TypeInitExpression expr, ExpressionCheckContext context)
        {
            throw new NotImplementedException();
        }

        public ExpressionType Visit(UnaryExpression expr, ExpressionCheckContext context)
        {
            ExpressionType operand = expr.Right.Accept(this, context);

            if (!context.CfgNode.Scope.IsOperatorDefined(Operator.GetOperation(expr.Op.Type), null, operand.Type))
                throw new OperatorNotDefinedException(expr);

            IOperator op = context.CfgNode.Scope.GetOperator(Operator.GetOperation(expr.Op.Type), null, operand.Type);

            expr.Type = op.ResultType;

            return new ExpressionType(op.ResultType, MutabilityUtils.GetResultMutability(operand.Mutability), null);
        }

        public ExpressionType Visit(GetExpression expr, ExpressionCheckContext context)
        {
            var obj = expr.Obj.Accept(this, context);

            if (obj.Type is NamespaceType namespaceType)
            {
                if (namespaceType.HasType(expr.Name.Lexeme))
                {
                    expr.Type = namespaceType.GetType(expr.Name.Lexeme);

                    //TODO: reference type symbol
                    return new ExpressionType(namespaceType.GetType(expr.Name.Lexeme), Mutability.CompileTimeConstantResult, null);
                }

                string resolvedName = namespaceType.ResolveName(expr.Name.Lexeme);
                if (!context.CfgNode.Scope.IsDefined(resolvedName))
                    throw new VarNotDefinedException(expr, resolvedName);

                Symbol var = context.CfgNode.Scope.GetVar(resolvedName);

                expr.Type = var.Type;
                return new ExpressionType(var.Type, Mutability.CompileTimeConstantResult, var);
            }

            if (!obj.Type.HasSymbol(expr.Name.Lexeme))
                throw new InvalidMemberException(expr);

            Symbol symbol = obj.Type.GetSymbol(expr.Name.Lexeme);

            expr.Type = symbol.Type;
            return new ExpressionType(symbol);
        }

        public ExpressionType Visit(GroupingExpression expr, ExpressionCheckContext context)
        {
            return expr.Inner.Accept(this, context);
        }

        public ExpressionType Visit(VarExpression expr, ExpressionCheckContext context)
        {
            if (!context.CfgNode.Scope.IsDefined(expr.Name.Lexeme))
                throw new VarNotDefinedException(expr);

            Symbol var = context.CfgNode.Scope.GetVar(expr.Name.Lexeme);
            expr.Type = var.Type;

            return new ExpressionType(var);
        }
    }
}
