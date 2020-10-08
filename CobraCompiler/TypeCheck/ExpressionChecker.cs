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
    class ExpressionChecker : IExpressionVisitorWithContext<(CobraType Type, Mutability Mutability), CFGNode>
    {
        public (CobraType, Mutability) Visit(AssignExpression expr, CFGNode cfgNode)
        {
            (CobraType varType, Mutability varMutability) = expr.Target.Accept(this, cfgNode);

            (CobraType assignType, Mutability assignMutability) = expr.Value.Accept(this, cfgNode);

            if (!assignType.CanCastTo(varType))
                throw new InvalidAssignmentException(expr);

            if(varMutability != Mutability.Mutable)
                throw new WriteToReadOnlySymbolException(expr);

            expr.Type = varType;

            return (varType, MutabilityUtils.GetResultMutability(varMutability, assignMutability));
        }

        public (CobraType, Mutability) Visit(BinaryExpression expr, CFGNode cfgNode)
        {
            var left = expr.Left.Accept(this, cfgNode);
            var right = expr.Right.Accept(this, cfgNode);

            if (!cfgNode.Scope.IsOperatorDefined(Operator.GetOperation(expr.Op.Type), left.Type, right.Type))
            {
                throw new OperatorNotDefinedException(expr);
            }

            IOperator op = cfgNode.Scope.GetOperator(Operator.GetOperation(expr.Op.Type), left.Type, right.Type);

            expr.Type = op.ResultType;

            return (op.ResultType, MutabilityUtils.GetResultMutability(left.Mutability, right.Mutability));
        }

        public (CobraType, Mutability) Visit(CallExpression expr, CFGNode cfgNode)
        {
            CobraType calleeType = expr.Callee.Accept(this, cfgNode).Type;
            List<CobraType> paramTypes = expr.Arguments.Select(arg => arg.Accept(this, cfgNode).Type).ToList();

            if (calleeType.IsCallable(paramTypes))
            {
                expr.Type = calleeType.CallReturn(paramTypes);
                return (calleeType.CallReturn(paramTypes), Mutability.Result);
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

        public (CobraType, Mutability) Visit(IndexExpression expr, CFGNode cfgNode)
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
                return (expr.Type, Mutability.Result);
            }

            if (collectionType is CobraTypeCobraType metaType && metaType.CobraType is CobraGeneric generic)
            {
                expr.Type = generic.CreateGenericInstance(typeParams);
                return (new CobraTypeCobraType(expr.Type), Mutability.CompileTimeConstantResult);
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

        public (CobraType, Mutability) Visit(ListLiteralExpression expr, CFGNode cfgNode)
        {
            (CobraType elementsCommonType, Mutability elementsMutability) = expr.Elements[0].Accept(this, cfgNode);

            foreach (Expression element in expr.Elements)
            {
                (CobraType elementType, Mutability elementMutability) = element.Accept(this, cfgNode);

                elementsCommonType = elementsCommonType.GetCommonParent(elementType);
                
                elementsMutability = MutabilityUtils.GetResultMutability(elementsMutability, elementMutability);
            }

            CobraType listType = DotNetCobraGeneric.ListType.CreateGenericInstance(new[] { elementsCommonType });

            expr.Type = listType;

            return (listType, elementsMutability);
        }

        public (CobraType, Mutability) Visit(LiteralExpression expr, CFGNode cfgNode)
        {
            return (expr.LiteralType, Mutability.CompileTimeConstantResult);
        }

        public (CobraType, Mutability) Visit(TypeInitExpression expr, CFGNode cfgNode)
        {
            throw new NotImplementedException();
        }

        public (CobraType, Mutability) Visit(UnaryExpression expr, CFGNode cfgNode)
        {
            var operand = expr.Right.Accept(this, cfgNode);

            if (!cfgNode.Scope.IsOperatorDefined(Operator.GetOperation(expr.Op.Type), null, operand.Type))
                throw new OperatorNotDefinedException(expr);

            IOperator op = cfgNode.Scope.GetOperator(Operator.GetOperation(expr.Op.Type), null, operand.Type);

            expr.Type = op.ResultType;

            return (op.ResultType, MutabilityUtils.GetResultMutability(operand.Mutability));
        }

        public (CobraType, Mutability) Visit(GetExpression expr, CFGNode cfgNode)
        {
            var obj = expr.Obj.Accept(this, cfgNode);

            if (obj.Type is NamespaceType namespaceType)
            {
                if (namespaceType.HasType(expr.Name.Lexeme))
                {
                    expr.Type = namespaceType.GetType(expr.Name.Lexeme);
                    return (namespaceType.GetType(expr.Name.Lexeme), Mutability.CompileTimeConstantResult);
                }

                string resolvedName = namespaceType.ResolveName(expr.Name.Lexeme);
                if (!cfgNode.Scope.IsDefined(resolvedName))
                    throw new VarNotDefinedException(expr, resolvedName);

                CobraType varType = cfgNode.Scope.GetVar(resolvedName).Type;

                expr.Type = varType;
                return (varType, Mutability.CompileTimeConstantResult);
            }

            if (!obj.Type.HasSymbol(expr.Name.Lexeme))
                throw new InvalidMemberException(expr);

            Symbol symbol = obj.Type.GetSymbol(expr.Name.Lexeme);

            expr.Type = symbol.Type;
            return (symbol.Type, symbol.Mutability);
        }

        public (CobraType, Mutability) Visit(GroupingExpression expr, CFGNode cfgNode)
        {
            return expr.Inner.Accept(this, cfgNode);
        }

        public (CobraType, Mutability) Visit(VarExpression expr, CFGNode cfgNode)
        {
            if (!cfgNode.Scope.IsDefined(expr.Name.Lexeme))
                throw new VarNotDefinedException(expr);

            var var = cfgNode.Scope.GetVar(expr.Name.Lexeme);
            expr.Type = var.Type;

            return (var.Type, var.Mutability);
        }
    }
}
