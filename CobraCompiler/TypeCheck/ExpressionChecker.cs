using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.CFG;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.TypeCheck.Exceptions;
using CobraCompiler.TypeCheck.Operators;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.TypeCheck
{
    class ExpressionChecker : IExpressionVisitorWithContext<CobraType, CFGNode>
    {
        public CobraType Visit(AssignExpression expr, CFGNode cfgNode)
        {
            CobraType varType = expr.Target.Accept(this, cfgNode);

            CobraType assignType = expr.Value.Accept(this, cfgNode);

            if (!assignType.CanCastTo(varType))
                throw new InvalidAssignmentException(expr);

            expr.Type = varType;

            return varType;
        }

        public CobraType Visit(BinaryExpression expr, CFGNode cfgNode)
        {
            CobraType leftType = expr.Left.Accept(this, cfgNode);
            CobraType rightType = expr.Right.Accept(this, cfgNode);

            if (!cfgNode.Scope.IsOperatorDefined(Operator.GetOperation(expr.Op.Type), leftType, rightType))
            {
                throw new OperatorNotDefinedException(expr);
            }

            IOperator op = cfgNode.Scope.GetOperator(Operator.GetOperation(expr.Op.Type), leftType, rightType);

            expr.Type = op.ResultType;

            return op.ResultType;
        }

        public CobraType Visit(CallExpression expr, CFGNode cfgNode)
        {
            CobraType calleeType = expr.Callee.Accept(this, cfgNode);
            List<CobraType> paramTypes = expr.Arguments.Select(arg => arg.Accept(this, cfgNode)).ToList();

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
                        CobraType test = expr.Arguments[i].Accept(this, cfgNode);
                        throw new InvalidArgumentException(expr.Arguments[i], func.OrderedTypeParams[i].Identifier);
                    }
                }
            }

            throw new InvalidCallException(expr);
        }

        public CobraType Visit(IndexExpression expr, CFGNode cfgNode)
        {
            CobraType collectionType = expr.Collection.Accept(this, cfgNode);

            List<CobraType> typeParams = new List<CobraType>();
            foreach (Expression expression in expr.Indicies)
            {
                CobraType exprType = expression.Accept(this, cfgNode);
                if (exprType is CobraTypeCobraType typeType && typeType.CobraType is CobraType simpleType)
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


            IOperator getOperator = CurrentcfgNode.Scope.GetOperator(Operation.Get, collectionType, DotNetCobraType.Int);

            expr.Type = getOperator.ResultType;

            return getOperator.ResultType;
            */
        }

        public CobraType Visit(ListLiteralExpression expr, CFGNode cfgNode)
        {
            CobraType elementsCommonType = expr.Elements[0].Accept(this, cfgNode);

            foreach (Expression element in expr.Elements)
            {
                elementsCommonType = elementsCommonType.GetCommonParent(element.Accept(this, cfgNode));
            }

            CobraType listType = DotNetCobraGeneric.ListType.CreateGenericInstance(new[] { elementsCommonType });

            expr.Type = listType;

            return listType;
        }

        public CobraType Visit(LiteralExpression expr, CFGNode cfgNode)
        {
            return expr.LiteralType;
        }

        public CobraType Visit(TypeInitExpression expr, CFGNode cfgNode)
        {
            throw new NotImplementedException();
        }

        public CobraType Visit(UnaryExpression expr, CFGNode cfgNode)
        {
            CobraType operand = expr.Right.Accept(this, cfgNode);

            if (!cfgNode.Scope.IsOperatorDefined(Operator.GetOperation(expr.Op.Type), null, operand))
                throw new OperatorNotDefinedException(expr);

            IOperator op = cfgNode.Scope.GetOperator(Operator.GetOperation(expr.Op.Type), null, operand);

            expr.Type = op.ResultType;

            return op.ResultType;
        }

        public CobraType Visit(GetExpression expr, CFGNode cfgNode)
        {
            CobraType objType = expr.Obj.Accept(this, cfgNode);

            if (objType is NamespaceType namespaceType)
            {
                if (namespaceType.HasType(expr.Name.Lexeme))
                {
                    expr.Type = namespaceType.GetType(expr.Name.Lexeme);
                    return namespaceType.GetType(expr.Name.Lexeme);
                }

                string resolvedName = namespaceType.ResolveName(expr.Name.Lexeme);
                if (!cfgNode.Scope.IsDefined(resolvedName))
                    throw new VarNotDefinedException(expr, resolvedName);

                CobraType varType = cfgNode.Scope.GetVar(resolvedName).Type;

                expr.Type = varType;
                return varType;
            }

            if (!objType.HasSymbol(expr.Name.Lexeme))
                throw new InvalidMemberException(expr);


            CobraType symbolType = objType.GetSymbol(expr.Name.Lexeme);

            expr.Type = symbolType;
            return symbolType;
        }

        public CobraType Visit(GroupingExpression expr, CFGNode cfgNode)
        {
            return expr.Inner.Accept(this, cfgNode);
        }

        public CobraType Visit(VarExpression expr, CFGNode cfgNode)
        {
            if (!cfgNode.Scope.IsDefined(expr.Name.Lexeme))
                throw new VarNotDefinedException(expr);

            CobraType varType = cfgNode.Scope.GetVar(expr.Name.Lexeme).Type;
            expr.Type = varType;

            return varType;
        }
    }
}
