using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Assemble.ExpressionAssemblyContexts;
using CobraCompiler.Parse;
using CobraCompiler.Parse.CFG;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Scanning;
using CobraCompiler.TypeCheck;
using CobraCompiler.TypeCheck.Operators;
using CobraCompiler.TypeCheck.Types;
using CobraCompiler.Util;
using InvalidOperationException = System.InvalidOperationException;

namespace CobraCompiler.Assemble
{
    class FuncAssembler: IExpressionVisitorWithContext<ExpressionAssemblyContext, ParentExpressionAssemblyContext>, IAssemble
    {
        private readonly MethodAttributes _methodAttributes;

        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly TypeBuilder _typeBuilder;
        private readonly TypeStore _typeStore;


        private ILGenerator _il;
        private readonly FuncScope _funcScope;
        private LocalManager _localManager;

        private readonly MethodStore _methodStore;

        private readonly Dictionary<GenericTypeParamPlaceholder, GenericTypeParameterBuilder> _funcGenerics;

        public FuncAssembler(FuncScope funcScope, TypeStore typeStore, MethodStore methodStore, TypeBuilder typeBuilder, AssemblyBuilder assemblyBuilder, MethodAttributes methodAttributes)
        {
            _funcScope = funcScope;
            _typeStore = typeStore;
            _methodStore = methodStore;
            _typeBuilder = typeBuilder;
            _assemblyBuilder = assemblyBuilder;
            _methodAttributes = methodAttributes;
            _funcGenerics = new Dictionary<GenericTypeParamPlaceholder, GenericTypeParameterBuilder>();
        }

        public MethodBase AssembleDefinition()
        {
            FuncDeclarationStatement funcDeclaration = _funcScope.FuncDeclaration;

            MethodBase methodBase = funcDeclaration is InitDeclarationStatement
                ? CreateConstructorBuilder(funcDeclaration)
                : CreateMethodBuilder(funcDeclaration);

            Type returnType = _funcScope.ReturnType != null ? _typeStore.GetType(_funcScope.ReturnType) : null;

            _localManager = new LocalManager(_funcScope, _typeStore, _il, returnType);

            _typeStore.PopGenerics(_funcGenerics);

            return methodBase;
        }

        private MethodBase CreateConstructorBuilder(FuncDeclarationStatement funcDeclaration)
        {
            Type[] paramTypes = _funcScope.Params.Select(param => _typeStore.GetType(param.Item2)).ToArray();

            ConstructorBuilder builder = _typeBuilder.DefineConstructor(_methodAttributes,CallingConventions.HasThis, paramTypes);

            for (int i = 0; i < funcDeclaration.Params.Count; i++)
                builder.DefineParameter(i + 1, ParameterAttributes.None, funcDeclaration.Params[i].Name.Lexeme);

            _il = builder.GetILGenerator();

            // Call object ctor
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Call, typeof(Object).GetConstructor(Type.EmptyTypes));

            return builder;
        }

        private MethodBase CreateMethodBuilder(FuncDeclarationStatement funcDeclaration)
        {
            MethodBuilder methodBuilder = _typeBuilder.DefineMethod(funcDeclaration.Name.Lexeme, _methodAttributes);

            if (funcDeclaration.TypeArguments.Count > 0)
            {
                GenericTypeParameterBuilder[] genericParams = methodBuilder.DefineGenericParameters(funcDeclaration.TypeArguments.Select(arg => arg.Lexeme).ToArray());
                GenericTypeParamPlaceholder[] placeholders = new GenericTypeParamPlaceholder[genericParams.Length];
                for (int i = 0; i < funcDeclaration.TypeArguments.Count; i++)
                {
                    _typeStore.AddType(_funcScope.GetType(new TypeInitExpression(new[] { funcDeclaration.TypeArguments[i] }, new TypeInitExpression[] { }, null)), genericParams[i]);
                    placeholders[i] = new GenericTypeParamPlaceholder(funcDeclaration.TypeArguments[i].Lexeme, i);
                    _funcGenerics[placeholders[i]] = genericParams[i];
                }


                _typeStore.PushCurrentGenerics(_funcGenerics);
            }

            Type[] paramTypes = _funcScope.Params.Select(param => _typeStore.GetType(param.Item2)).ToArray();
            Type returnType = _funcScope.ReturnType != null ? _typeStore.GetType(_funcScope.ReturnType) : null;

            methodBuilder.SetReturnType(returnType);
            methodBuilder.SetParameters(paramTypes);

            if (funcDeclaration.Name.Lexeme == "main")
                _assemblyBuilder.SetEntryPoint(methodBuilder);

            for (int i = 0; i < funcDeclaration.Params.Count; i++)
                methodBuilder.DefineParameter(i + 1, ParameterAttributes.None, funcDeclaration.Params[i].Name.Lexeme);

            _il = methodBuilder.GetILGenerator();

            return methodBuilder;
        }

        public void Assemble()
        {
            _typeStore.PushCurrentGenerics(_funcGenerics);

            ListNibbler<CFGNode> nodes = new ListNibbler<CFGNode>(_funcScope.CFGraph.CFGNodes);

            do
            {
                nodes.Pop();
                AssembleNode(nodes, _typeBuilder);

            } while (nodes.HasNext());

            //TODO: track more efficiently
            bool explicitReturn = false;
            foreach (CFGNode node in _funcScope.CFGraph.Terminal.Previous)
                explicitReturn = node.Statements.Any(x => x is ReturnStatement);

            if(_funcScope.FuncDeclaration is InitDeclarationStatement || !explicitReturn)
                _il.Emit(OpCodes.Ret);

            _typeStore.PopGenerics(_funcGenerics);
        }

        private void AssembleNode(ListNibbler<CFGNode> cfgNodes, TypeBuilder typeBuilder)
        {
            if (!cfgNodes.HasNext())
                return;

            foreach (Statement statement in cfgNodes.Peek().Statements)
            {
                AssembleStatement(statement, cfgNodes, typeBuilder);
            }
        }

        private void AssembleStatement(Statement statement, ListNibbler<CFGNode> cfgNodes, TypeBuilder typeBuilder)
        {
            if (!cfgNodes.HasNext())
                return;

            switch (statement)
            {
                case BlockStatement block:
                    break;
                case ExpressionStatement exprStmt:
                    exprStmt.Expression.Accept(this, new ParentExpressionAssemblyContext(cfgNodes.Peek().Scope));
                    break;
                case VarDeclarationStatement varDeclaration:
                    CobraType varType = cfgNodes.Peek().Scope.GetVar(varDeclaration.Name.Lexeme).Type;
                    _localManager.DeclareVar(cfgNodes.Peek().Scope, varDeclaration.Name.Lexeme, _typeStore.GetType(varType));
                    varDeclaration.Assignment?.Accept(this, new ParentExpressionAssemblyContext(cfgNodes.Peek().Scope));
                    break;
                case ReturnStatement returnStatement:
                    returnStatement.Value.Accept(this, new ParentExpressionAssemblyContext(cfgNodes.Peek().Scope));
                    ReturnStatement(cfgNodes.Peek().Scope);
                    break;
                case IfStatement ifStatement:
                    AssembleIfStatement(ifStatement, cfgNodes, typeBuilder);
                    break;
                case WhileStatement whileStatement:
                    AssembleWhileStatement(whileStatement, cfgNodes, typeBuilder);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void AssembleIfStatement(IfStatement ifStatement, ListNibbler<CFGNode> cfgNodes, TypeBuilder typeBuilder)
        {
            ifStatement.Condition.Accept(this, new ParentExpressionAssemblyContext(cfgNodes.Peek().Scope));

            Label elseLabel = _il.DefineLabel();
            Label endElseLabel = _il.DefineLabel();

            _il.Emit(OpCodes.Brfalse, elseLabel);
            cfgNodes.Pop();
            AssembleNodesInScope(cfgNodes, typeBuilder);

            bool ifReturns = cfgNodes.Peek().FulfilledByChildren(node => node.Scope == cfgNodes.Peek().Scope && (node.Next.OnlyOrDefault()?.IsTerminal ?? false));

            if (!ifReturns)
                _il.Emit(OpCodes.Br, endElseLabel);


            _il.MarkLabel(elseLabel);
            if (ifStatement.Else != null)
            {
                cfgNodes.Pop();
                AssembleNodesInScope(cfgNodes, typeBuilder);
            }

            if (!ifReturns)
            {
                _il.MarkLabel(endElseLabel);
                _il.Emit(OpCodes.Nop);
            }
        }

        private void AssembleWhileStatement(WhileStatement whileStatement, ListNibbler<CFGNode> cfgNodes, TypeBuilder typeBuilder)
        {
            Label elseLabel = _il.DefineLabel();
            Label endElseLabel = _il.DefineLabel();
            Label whileLabel = _il.DefineLabel();
            Label bodyLabel = _il.DefineLabel();

            whileStatement.Condition.Accept(this, new ParentExpressionAssemblyContext(cfgNodes.Peek().Scope));

            _il.Emit(OpCodes.Brfalse, elseLabel); // If condition fails the first time go to else
            _il.Emit(OpCodes.Br, bodyLabel); // Else go to body
            _il.MarkLabel(whileLabel); // Return here on subsequent loop

            whileStatement.Condition.Accept(this, new ParentExpressionAssemblyContext(cfgNodes.Peek().Scope));

            _il.Emit(OpCodes.Brfalse, endElseLabel);
            _il.MarkLabel(bodyLabel); // Beginning of body

            cfgNodes.Pop();
            AssembleNodesInScope(cfgNodes, typeBuilder);

            _il.Emit(OpCodes.Br, whileLabel); // Return to condition
            _il.MarkLabel(elseLabel);
            if (whileStatement.Else != null)
            {
                cfgNodes.Pop();
                AssembleNodesInScope(cfgNodes, typeBuilder);
            }
            _il.MarkLabel(endElseLabel);
            _il.Emit(OpCodes.Nop);
        }

        private void AssembleNodesInScope(ListNibbler<CFGNode> nodes, TypeBuilder typeBuilder)
        {
            Scope scope = nodes.Peek().Scope;

            if (!nodes.HasNext())
                throw new ArgumentException("nodes does not contain enough nodes!", nameof(nodes));

            AssembleNode(nodes, typeBuilder);

            while (nodes.Peek(1).Scope.IsContainedBy(scope))
            {
                nodes.Pop();
                AssembleNode(nodes, typeBuilder);
            }
        }

        private void ReturnStatement(Scope scope)
        {
            if (_funcScope.ReturnType != DotNetCobraType.Unit)
            {
                _localManager.StoreVar(scope, "@ret");
                _localManager.LoadVar(scope, "@ret");
            }
            
            _il.Emit(OpCodes.Ret);
        }


        public ExpressionAssemblyContext Visit(AssignExpression expr, ParentExpressionAssemblyContext context)
        {
            ExpressionAssemblyContext targetContext = expr.Target.Accept(this, new ParentExpressionAssemblyContext(context.Scope, assigning: true));
            ExpressionAssemblyContext valContext = expr.Value.Accept(this, new ParentExpressionAssemblyContext(context.Scope, expected: targetContext.Type));
            
            if(expr.Target is VarExpression varExpr)
                _localManager.StoreVar(context.Scope, varExpr.Name.Lexeme);

            else if (targetContext.AssigningToField)
                _il.Emit(OpCodes.Stfld, targetContext.AssignToField);

            else if(targetContext.AssigningToIndex)
                _il.Emit(OpCodes.Callvirt, targetContext.AssignToIndex);

            return new ExpressionAssemblyContext(targetContext.Type);
        }

        public ExpressionAssemblyContext Visit(BinaryExpression expr, ParentExpressionAssemblyContext context)
        {
            CobraType leftType = expr.Left.Accept(this, new ParentExpressionAssemblyContext(context.Scope)).Type;
            CobraType rightType = expr.Right.Accept(this, new ParentExpressionAssemblyContext(context.Scope)).Type;

            IOperator op = _funcScope.GetOperator(Operator.GetOperation(expr.Op.Type), leftType, rightType);
            if (op is DotNetBinaryOperator dotNetBinaryOperator)
            {
                _il.Emit(dotNetBinaryOperator.OpCode);
            }

            return new ExpressionAssemblyContext(op.ResultType);
        }

        public ExpressionAssemblyContext Visit(CallExpression expr, ParentExpressionAssemblyContext context)
        {
            //Used for overload resolution
            List<CobraType> sig = new List<CobraType>();
            foreach (Expression arg in expr.Arguments)
                sig.Add(arg.Type);

            sig.Add(expr.Type);

            CobraType expected = DotNetCobraGeneric.FuncType.CreateGenericInstance(sig);

            ExpressionAssemblyContext calleeContext = expr.Callee.Accept(this, new ParentExpressionAssemblyContext(context.Scope, calling: true, expected: expected));
            for (int i = 0; i < expr.Arguments.Count; i++)
                expr.Arguments[i].Accept(this, new ParentExpressionAssemblyContext(context.Scope, expected: sig[i]));
            
            CobraType returnType = null;

            if (calleeContext is MethodBuilderExpressionAssemblyContext methodExpressionContext)
            {
                if (methodExpressionContext.Method.IsGenericMethodDefinition)
                {
                    MethodInfo genericMethod = (MethodInfo) methodExpressionContext.Method;
                    genericMethod = genericMethod.MakeGenericMethod();
                    //TypeBuilder.GetMethod(typeof(int), genericMethod.);

                    throw new NotImplementedException();
                }

                switch (methodExpressionContext.Method)
                {
                    case ConstructorInfo constructorInfo:
                        _il.Emit(OpCodes.Newobj, constructorInfo);
                        break;
                    case MethodInfo methodInfo:
                        _il.Emit(OpCodes.Call, methodInfo);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(methodExpressionContext.Method));
                }
            }
            else
            {
                Type type = _typeStore.GetType(calleeContext.Type);
                type.GetGenericTypeDefinition();
                MethodInfo methodInfo = type.ContainsGenericParameters ?  TypeBuilder.GetMethod(type, type.GetGenericTypeDefinition().GetMethod("Invoke")): type.GetMethod("Invoke");
                _il.Emit(OpCodes.Callvirt, methodInfo);
            }
            

            if (calleeContext.Type is CobraGenericInstance funcInstance)
                returnType = funcInstance.OrderedTypeParams.Last();

            return new ExpressionAssemblyContext(returnType);
        }

        public ExpressionAssemblyContext Visit(IndexExpression expr, ParentExpressionAssemblyContext context)
        {
            ExpressionAssemblyContext collectionContext = expr.Collection.Accept(this, new ParentExpressionAssemblyContext(context.Scope, expected: expr.Collection.Type, calling:context.ImmediatelyCalling));

            // If this is specifying type args to a function or constructor
            if (expr.Type is CobraGenericInstance genericInstance && collectionContext is MethodBuilderExpressionAssemblyContext methodContext)
            {
                MethodBase genericMethodInfo = methodContext.Method;
                Type[] typeArgs = expr.Indicies.Select(e => _typeStore.GetType(((CobraTypeCobraType) e.Type).CobraType)).ToArray();

                if (genericMethodInfo is MethodInfo methodInfo)
                {
                    genericMethodInfo = methodInfo.MakeGenericMethod(typeArgs);
                }

                if (genericMethodInfo is ConstructorInfo constructorInfo)
                {
                    genericMethodInfo = TypeBuilder.GetConstructor(constructorInfo.DeclaringType.MakeGenericType(typeArgs), constructorInfo);
                }

                return new MethodBuilderExpressionAssemblyContext(expr.Type, genericMethodInfo);
            }

            // If this is an index into a collection
            List<CobraType> indexTypes = expr.Indicies.Select((x) => x.Accept(this, context).Type).ToList();

            string methodName = context.Assigning ? "set_Item" : "get_Item";

            Symbol symbol = collectionContext.Type.GetSymbol(methodName);

            Type collectionType = _typeStore.GetType(collectionContext.Type);

            MethodInfo indexMethod = ResolveGenericMethodInfo(collectionType, collectionType.GetGenericTypeDefinition().GetMethod(methodName));

            if (context.Assigning)
                return new ExpressionAssemblyContext(DotNetCobraType.Unit, indexMethod);

            _il.Emit(OpCodes.Callvirt, indexMethod);

            return new ExpressionAssemblyContext(symbol.Type.CallReturn(indexTypes));
        }

        public ExpressionAssemblyContext Visit(ListLiteralExpression expr, ParentExpressionAssemblyContext context)
        {
            CobraType type = context.ExpectedType;

            Type listType = _typeStore.GetType(type);

            CobraType elementType = (type as CobraGenericInstance).OrderedTypeParams[0];

            ConstructorInfo listCtor = ResolveGenericConstructorInfo(listType, new[] {typeof(int)});
            MethodInfo addMethod = ResolveGenericMethodInfo(listType, listType.GetGenericTypeDefinition().GetMethod("Add"));

            _localManager.LoadLiteral(expr.Elements.Count);
            _il.Emit(OpCodes.Newobj, listCtor);

            foreach (Expression element in expr.Elements)
            {
                _il.Emit(OpCodes.Dup);
                element.Accept(this, new ParentExpressionAssemblyContext(context.Scope, expected:elementType));
                _il.Emit(OpCodes.Callvirt, addMethod);
            }

            return new ExpressionAssemblyContext(type);
        }

        public ExpressionAssemblyContext Visit(LiteralExpression expr, ParentExpressionAssemblyContext context)
        {
            _localManager.LoadLiteral(expr.Value);

            return new ExpressionAssemblyContext(expr.LiteralType);
        }

        public ExpressionAssemblyContext Visit(TypeInitExpression expr, ParentExpressionAssemblyContext context)
        {
            throw new NotImplementedException();
        }

        public ExpressionAssemblyContext Visit(UnaryExpression expr, ParentExpressionAssemblyContext context)
        {
            CobraType operandType = expr.Right.Accept(this, new ParentExpressionAssemblyContext(context.Scope)).Type;

            IOperator op = _funcScope.GetOperator(Operator.GetOperation(expr.Op.Type), null, operandType);
            if (op is DotNetBinaryOperator dotNetBinaryOperator)
            {
                _il.Emit(dotNetBinaryOperator.OpCode);
            }

            return new ExpressionAssemblyContext(op.ResultType);
        }

        public ExpressionAssemblyContext Visit(GroupingExpression expr, ParentExpressionAssemblyContext context)
        {
            return expr.Inner.Accept(this, new ParentExpressionAssemblyContext(context.Scope));
        }

        public ExpressionAssemblyContext Visit(VarExpression expr, ParentExpressionAssemblyContext context)
        {
            CobraType varType = context.Scope.GetVar(expr.Name.Lexeme).Type;

            if(varType is NamespaceType)
                return new ExpressionAssemblyContext(varType);

            if (_methodStore.HasMethodBuilder(expr.Name.Lexeme))
            {
                string varId = expr.Name.Lexeme;
                MethodBase method = _methodStore.GetMethodInfo(context.ExpectedType, varId);

                MethodBuilderExpressionAssemblyContext mbContext =
                    new MethodBuilderExpressionAssemblyContext(varType, method);

                if (context.ImmediatelyCalling)
                    return mbContext;
                
                _il.Emit(OpCodes.Ldnull);

                switch (method)
                {
                    case ConstructorInfo constructorInfo:
                        _il.Emit(OpCodes.Ldftn, constructorInfo);
                        break;
                    case MethodInfo methodInfo:
                        _il.Emit(OpCodes.Ldftn, methodInfo);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Type[] types = { typeof(object), typeof(IntPtr) };
                ConstructorInfo ctor = ResolveGenericConstructorInfo(_typeStore.GetType(mbContext.Type), types);

                _il.Emit(OpCodes.Newobj, ctor);
            }
            else if (!context.Assigning)
            {
                if (context.CallingMember && _typeStore.GetType(varType).IsValueType)
                    _localManager.LoadVarAddress(context.Scope, expr.Name.Lexeme);
                else
                    _localManager.LoadVar(context.Scope, expr.Name.Lexeme);
            }
            else
                _localManager.PrepStoreField(expr.Name.Lexeme);

            return new ExpressionAssemblyContext(varType);
        }

        public ExpressionAssemblyContext Visit(GetExpression expr, ParentExpressionAssemblyContext context)
        {
            ExpressionAssemblyContext exprContext = expr.Obj.Accept(this, new ParentExpressionAssemblyContext(context.Scope, callingMember:context.ImmediatelyCalling));
            if (exprContext.Type is NamespaceType _namespace)
            {
                if(_namespace.HasType(expr.Name.Lexeme))
                    return new ExpressionAssemblyContext(_namespace.GetType(expr.Name.Lexeme));

                Token resolvedToken = new Token(TokenType.Identifier, _namespace.ResolveName(expr.Name.Lexeme), null,
                    expr.Name.SourceLocation, null);

                return Visit(new VarExpression(resolvedToken), context);
            }

            //TODO implement class method overloading
            Type varType = _typeStore.GetType(exprContext.Type);
            MemberInfo member = _typeStore.GetMemberInfo(exprContext.Type, expr.Name.Lexeme, context.ExpectedType);

            switch (member)
            {
                case PropertyInfo prop:
                    MethodInfo get = ResolveGenericMethodInfo(varType, prop.GetGetMethod());
                    _il.Emit(OpCodes.Callvirt, get);
                    break;
                case FieldInfo field:
                    if(!context.Assigning)
                        _il.Emit(OpCodes.Ldfld, field);
                    else 
                        return new ExpressionAssemblyContext(exprContext.Type.GetSymbol(expr.Name.Lexeme).Type, field);
                    break;
                case MethodInfo method:
                    method = ResolveGenericMethodInfo(varType, method);
                    if (context.ImmediatelyCalling)
                        return new MethodBuilderExpressionAssemblyContext(exprContext.Type.GetSymbol(expr.Name.Lexeme).Type, method);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return new ExpressionAssemblyContext(exprContext.Type.GetSymbol(expr.Name.Lexeme).Type);
        }

        private MethodInfo ResolveGenericMethodInfo(Type type, MethodInfo methodInfo)
        {
            if (!type.IsConstructedGenericType)
                return methodInfo;

            bool includesTypeBuilder = false;
            foreach (Type genericArgument in type.GetGenericArguments())
            {
                includesTypeBuilder = includesTypeBuilder || genericArgument is TypeBuilder || genericArgument is GenericTypeParameterBuilder;
            }

            if(includesTypeBuilder || (!methodInfo.DeclaringType.IsConstructedGenericType && type.Assembly == _assemblyBuilder))
                return TypeBuilder.GetMethod(type, methodInfo);

            if (!methodInfo.DeclaringType.IsConstructedGenericType)
                return type.GetMethod(methodInfo.Name);

            return methodInfo;
        }

        private ConstructorInfo ResolveGenericConstructorInfo(Type type, Type[] ctorArgs)
        {
            if (!type.IsConstructedGenericType)
                return type.GetConstructor(ctorArgs);

            bool includesTypeBuilder = false;
            foreach (Type genericArgument in type.GetGenericArguments())
            {
                includesTypeBuilder = includesTypeBuilder || genericArgument is TypeBuilder || genericArgument is GenericTypeParameterBuilder;
            }

            ConstructorInfo genericConstructorInfo = type.GetGenericTypeDefinition().GetConstructor(ctorArgs);

            if (includesTypeBuilder)
                return TypeBuilder.GetConstructor(type, genericConstructorInfo);

            return type.GetConstructor(ctorArgs);
        }
    }
}
