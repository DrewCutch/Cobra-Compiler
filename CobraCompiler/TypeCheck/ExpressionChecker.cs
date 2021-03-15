using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.CFG;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;
using CobraCompiler.TypeCheck.Assertion;
using CobraCompiler.TypeCheck.CFG;
using CobraCompiler.TypeCheck.Exceptions;
using CobraCompiler.TypeCheck.Operators;
using CobraCompiler.TypeCheck.Symbols;
using CobraCompiler.TypeCheck.Types;
using CobraCompiler.Util;

namespace CobraCompiler.TypeCheck
{
    class ExpressionChecker : IExpressionVisitorWithContext<ExpressionType, ExpressionChecker.ExpressionCheckContext>
    {
        public class ExpressionCheckContext
        {
            public readonly CFGNode CfgNode;
            public readonly CobraType Assigning;
            public readonly CobraType Expected;
            public readonly bool IsInitialPass;

            public ExpressionCheckContext(CFGNode cfgNode, CobraType expected = null, CobraType assigning = null, bool isInitialPass = false)
            {
                CfgNode = cfgNode;
                Assigning = assigning;
                Expected = expected;
                IsInitialPass = isInitialPass;
            }
        }

        public ExpressionType Visit(AssignExpression expr, ExpressionCheckContext context)
        {
            ExpressionType initialVar = expr.Target.Accept(this, new ExpressionCheckContext(context.CfgNode, isInitialPass: true));

            ExpressionType value = expr.Value.Accept(this, new ExpressionCheckContext(context.CfgNode, expected: initialVar.Type));

            ExpressionType var = expr.Target.Accept(this, new ExpressionCheckContext(context.CfgNode, assigning: value.Type));

            if (!value.Type.CanCastTo(var.Type))
                throw new InvalidAssignmentException(expr);

            if (var.Symbol?.Kind == SymbolKind.Param)
                throw new AssignToParamException(expr, var.Symbol);

            if(var.Mutability != Mutability.Mutable && var.Mutability != Mutability.AssignOnce)
                throw new WriteToReadOnlySymbolException(expr);

            if(var.Mutability == Mutability.AssignOnce && context.CfgNode.FulfilledByAnyAncestors(ControlFlowCheck.IsAssigned(var.Symbol)))
                throw new WriteToReadOnlySymbolException(expr);

            expr.Type = var.Type;

            if (var.Symbol != null) 
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
                if (func.TypeArguments.Count - 1 != expr.Arguments.Count)
                {
                    throw new IncorrectArgumentCountException(expr, func.TypeParams.Count - 1);
                }

                for (int i = 0; i < func.TypeArguments.Count - 1; i++)
                {
                    if (!paramTypes[i].CanCastTo(func.OrderedTypeArguments[i]))
                    {
                        throw new InvalidArgumentException(expr.Arguments[i], func.OrderedTypeArguments[i].Identifier);
                    }
                }
            }

            throw new InvalidCallException(expr);
        }

        private ExpressionType VisitTypeDeclaration(IndexExpression expr, CobraType collectionType, ExpressionCheckContext context)
        {
            List<CobraType> typeParams = new List<CobraType>();

            foreach (Expression expression in expr.Indicies)
            {
                CobraType exprType = expression.Accept(this, context).Type;
                if (exprType is CobraTypeCobraType typeType && typeType.CobraType is CobraType simpleType)
                    typeParams.Add(simpleType);
                else
                    throw new InvalidGenericArgumentException(expression);
            }

            if (collectionType.IsConstructedGeneric && collectionType.HasPlaceholders)
            {
                expr.Type = collectionType.ReplacePlaceholders(typeParams);
                return new ExpressionType(expr.Type, Mutability.Result, null);
            }

            if (collectionType is CobraTypeCobraType metaType && metaType.CobraType.IsGenericType)
            {
                expr.Type = metaType.CobraType.CreateGenericInstance(typeParams);
                return new ExpressionType(new CobraTypeCobraType(expr.Type), Mutability.CompileTimeConstantResult, null);
            }

            throw new NotImplementedException();
        }

        public ExpressionType Visit(IndexExpression expr, ExpressionCheckContext context)
        {
            CobraType collectionType = expr.Collection.Accept(this, context).Type;

            bool couldBeTypeDeclaration =
                (collectionType.IsConstructedGeneric && collectionType.HasPlaceholders) ||
                (collectionType is CobraTypeCobraType metaType && metaType.CobraType.IsGenericType);

            // It is not a type declaration
            List<CobraType> indexTypes = expr.Indicies.Select((x) => x.Accept(this, context).Type).ToList();

            if(context.Assigning != null)
                indexTypes.Add(context.Assigning);

            string symbolName = context.Assigning != null ? "set_Item" : "get_Item";

            Symbol symbol = collectionType.GetSymbol(symbolName);

            if(symbol == null)
                return couldBeTypeDeclaration ? VisitTypeDeclaration(expr, collectionType, context) : throw new InvalidIndexException(expr);

            CobraType valueType = symbol.Type.CallReturn(indexTypes);

            if(valueType == null)
                return couldBeTypeDeclaration ? VisitTypeDeclaration(expr, collectionType, context) : throw new InvalidIndexException(expr);

            expr.Type = valueType;

            return new ExpressionType(context.Assigning ?? valueType, context.Assigning != null ? Mutability.Mutable : Mutability.Result, null);
        }

        public ExpressionType Visit(ListLiteralExpression expr, ExpressionCheckContext context)
        {
            if (expr.Elements.Empty())
            {
                return new ExpressionType(context.Expected, Mutability.Result, null);
            }

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

        public ExpressionType Visit(NullableAccessExpression expr, ExpressionCheckContext context)
        {
            //If is actually a nullable expression
            if (expr.Name == null)
            {
                ExpressionType exprType = expr.Obj.Accept(this, context);

                if (exprType.Type is CobraTypeCobraType metaType)
                {
                    CobraTypeCobraType nullableType = new CobraTypeCobraType(CobraType.Nullable(metaType.CobraType));
                    expr.Type = nullableType;
                    return new ExpressionType(nullableType,  Mutability.CompileTimeConstantResult, null);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            return VisitMemberAccessExpression(expr, true, context);
        }

        public ExpressionType Visit(TypeInitExpression expr, ExpressionCheckContext context)
        {
            throw new NotImplementedException();
        }

        public ExpressionType Visit(TypeAssertionExpression expr, ExpressionCheckContext context)
        {
            ExpressionType left = expr.Left.Accept(this, context);
            ExpressionType right = expr.Right.Accept(this, context);

            if(!(right.Type is CobraTypeCobraType) && right.Type != DotNetCobraType.Null)
                throw new NotImplementedException("type assertions must be against a type");

            CobraType assertType = (right.Type as CobraTypeCobraType)?.CobraType ?? DotNetCobraType.Null;

            if (expr.NotType)
                assertType = left.Type.NullableBase;

            // Types can only be asserted for non member readonly variables
            bool canAssert = expr.Left is VarExpression && left.Mutability != Mutability.Mutable;

            expr.Type = DotNetCobraType.Bool;

            TypeAssertion typeAssertion = null;

            if (canAssert)
            {
                typeAssertion = new TypeAssertion(expr, left.Symbol, assertType);
            }

            return new ExpressionType(DotNetCobraType.Bool, Mutability.Result, null, typeAssertion != null ? new List<TypeAssertion>{typeAssertion} : new List<TypeAssertion>());
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
            return VisitMemberAccessExpression(expr, false, context);
        }

        private ExpressionType VisitMemberAccessExpression(MemberAccessExpression expr, bool nullSafe, ExpressionCheckContext context)
        {
            var obj = expr.Obj.Accept(this, context);

            if (!obj.Type.IsNullable && nullSafe)
                throw new InvalidNullableAccessException(expr);

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

            CobraType objType = obj.Type;

            if (objType.IsNullable && nullSafe)
                objType = objType.NullableBase;

            if (objType.IsNullable)
                throw new InvalidMemberAccessOnNullableException(expr);

            if (!objType.HasSymbol(expr.Name.Lexeme))
                throw new InvalidMemberException(expr);

            Symbol symbol = objType.GetSymbol(expr.Name.Lexeme);

            CobraType exprType = objType.IsNullable ? CobraType.Nullable(symbol.Type) : symbol.Type;

            expr.Type = exprType;
            return new ExpressionType(exprType, nullSafe ? Mutability.Result : symbol.Mutability, symbol);
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

            if (!context.IsInitialPass && context.CfgNode.Graph != null && context.Assigning == null && !context.CfgNode.FulfilledByAncestors(ControlFlowCheck.IsAssigned(var)))
                throw new UnassignedVarException(expr);

            return new ExpressionType(var);
        }
    }
}
