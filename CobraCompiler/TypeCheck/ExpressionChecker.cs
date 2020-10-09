using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.CFG;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.TypeCheck.Exceptions;
using CobraCompiler.TypeCheck.Operators;
using CobraCompiler.TypeCheck.Symbols;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.TypeCheck
{
    class ExpressionChecker : IExpressionVisitorWithContext<ExpressionType, CFGNode>
    {
        public ExpressionType Visit(AssignExpression expr, CFGNode cfgNode)
        {
            ExpressionType var = expr.Target.Accept(this, cfgNode);

            ExpressionType value = expr.Value.Accept(this, cfgNode);

            if (!value.Type.CanCastTo(var.Type))
                throw new InvalidAssignmentException(expr);

            if (var.Symbol?.Kind == SymbolKind.Param)
                throw new AssignToParamException(expr, var.Symbol);

            if(var.Mutability != Mutability.Mutable && var.Mutability != Mutability.AssignOnce)
                throw new WriteToReadOnlySymbolException(expr);

            if(var.Mutability == Mutability.AssignOnce && cfgNode.IsEverAssigned(var.Symbol))
                throw new WriteToReadOnlySymbolException(expr);

            expr.Type = var.Type;

            cfgNode.AddAssignment(var.Symbol, expr);

            return new ExpressionType(var.Type, MutabilityUtils.GetResultMutability(var.Mutability, value.Mutability),
                null);
        }

        public ExpressionType Visit(BinaryExpression expr, CFGNode cfgNode)
        {
            ExpressionType left = expr.Left.Accept(this, cfgNode);
            ExpressionType right = expr.Right.Accept(this, cfgNode);

            if (!cfgNode.Scope.IsOperatorDefined(Operator.GetOperation(expr.Op.Type), left.Type, right.Type))
            {
                throw new OperatorNotDefinedException(expr);
            }

            IOperator op = cfgNode.Scope.GetOperator(Operator.GetOperation(expr.Op.Type), left.Type, right.Type);

            expr.Type = op.ResultType;

            return new ExpressionType(op.ResultType,
                MutabilityUtils.GetResultMutability(left.Mutability, right.Mutability), null);
        }

        public ExpressionType Visit(CallExpression expr, CFGNode cfgNode)
        {
            CobraType calleeType = expr.Callee.Accept(this, cfgNode).Type;
            List<CobraType> paramTypes = expr.Arguments.Select(arg => arg.Accept(this, cfgNode).Type).ToList();

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

        public ExpressionType Visit(IndexExpression expr, CFGNode cfgNode)
        {
            CobraType collectionType = expr.Collection.Accept(this, cfgNode).Type;

            List<CobraType> typeParams = new List<CobraType>();
            foreach (Expression expression in expr.Indicies)
            {
                CobraType exprType = expression.Accept(this, cfgNode).Type;
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

        public ExpressionType Visit(ListLiteralExpression expr, CFGNode cfgNode)
        {
            ExpressionType firstElement = expr.Elements[0].Accept(this, cfgNode);
            CobraType elementsCommonType = firstElement.Type;
            Mutability elementsMutability = firstElement.Mutability;

            foreach (Expression element in expr.Elements)
            {
                ExpressionType elementExpressionType = element.Accept(this, cfgNode);

                elementsCommonType = elementsCommonType.GetCommonParent(elementExpressionType.Type);
                
                elementsMutability = MutabilityUtils.GetResultMutability(elementsMutability, elementExpressionType.Mutability);
            }

            CobraType listType = DotNetCobraGeneric.ListType.CreateGenericInstance(new[] { elementsCommonType });

            expr.Type = listType;

            return new ExpressionType(listType, elementsMutability, null);
        }

        public ExpressionType Visit(LiteralExpression expr, CFGNode cfgNode)
        {
            return new ExpressionType(expr.LiteralType, Mutability.CompileTimeConstantResult, null);
        }

        public ExpressionType Visit(TypeInitExpression expr, CFGNode cfgNode)
        {
            throw new NotImplementedException();
        }

        public ExpressionType Visit(UnaryExpression expr, CFGNode cfgNode)
        {
            ExpressionType operand = expr.Right.Accept(this, cfgNode);

            if (!cfgNode.Scope.IsOperatorDefined(Operator.GetOperation(expr.Op.Type), null, operand.Type))
                throw new OperatorNotDefinedException(expr);

            IOperator op = cfgNode.Scope.GetOperator(Operator.GetOperation(expr.Op.Type), null, operand.Type);

            expr.Type = op.ResultType;

            return new ExpressionType(op.ResultType, MutabilityUtils.GetResultMutability(operand.Mutability), null);
        }

        public ExpressionType Visit(GetExpression expr, CFGNode cfgNode)
        {
            var obj = expr.Obj.Accept(this, cfgNode);

            if (obj.Type is NamespaceType namespaceType)
            {
                if (namespaceType.HasType(expr.Name.Lexeme))
                {
                    expr.Type = namespaceType.GetType(expr.Name.Lexeme);

                    //TODO: reference type symbol
                    return new ExpressionType(namespaceType.GetType(expr.Name.Lexeme), Mutability.CompileTimeConstantResult, null);
                }

                string resolvedName = namespaceType.ResolveName(expr.Name.Lexeme);
                if (!cfgNode.Scope.IsDefined(resolvedName))
                    throw new VarNotDefinedException(expr, resolvedName);

                Symbol var = cfgNode.Scope.GetVar(resolvedName);

                expr.Type = var.Type;
                return new ExpressionType(var.Type, Mutability.CompileTimeConstantResult, var);
            }

            if (!obj.Type.HasSymbol(expr.Name.Lexeme))
                throw new InvalidMemberException(expr);

            Symbol symbol = obj.Type.GetSymbol(expr.Name.Lexeme);

            expr.Type = symbol.Type;
            return new ExpressionType(symbol);
        }

        public ExpressionType Visit(GroupingExpression expr, CFGNode cfgNode)
        {
            return expr.Inner.Accept(this, cfgNode);
        }

        public ExpressionType Visit(VarExpression expr, CFGNode cfgNode)
        {
            if (!cfgNode.Scope.IsDefined(expr.Name.Lexeme))
                throw new VarNotDefinedException(expr);

            Symbol var = cfgNode.Scope.GetVar(expr.Name.Lexeme);
            expr.Type = var.Type;

            return new ExpressionType(var);
        }
    }
}
