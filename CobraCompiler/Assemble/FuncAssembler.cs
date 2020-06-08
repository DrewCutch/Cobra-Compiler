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
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Scanning;
using CobraCompiler.TypeCheck;
using CobraCompiler.TypeCheck.Operators;
using CobraCompiler.TypeCheck.Types;
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

        private readonly ScopeCrawler _scopeCrawler;
        private Scope CurrentScope => _scopeCrawler.Current;

        private readonly MethodStore _methodStore;

        public FuncAssembler(FuncScope funcScope, TypeStore typeStore, MethodStore methodStore, TypeBuilder typeBuilder, AssemblyBuilder assemblyBuilder, MethodAttributes methodAttributes)
        {
            _funcScope = funcScope;
            _typeStore = typeStore;
            _methodStore = methodStore;
            _typeBuilder = typeBuilder;
            _assemblyBuilder = assemblyBuilder;
            _methodAttributes = methodAttributes;
            _scopeCrawler = new ScopeCrawler(funcScope);
        }

        public MethodBase AssembleDefinition()
        {
            FuncDeclarationStatement funcDeclaration = _funcScope.FuncDeclaration;

            Type[] paramTypes = _funcScope.Params.Select(param => _typeStore.GetType(param.Item2)).ToArray();
            Type returnType = _funcScope.ReturnType != null ? _typeStore.GetType(_funcScope.ReturnType) : null;

            MethodBase methodBase = funcDeclaration is InitDeclarationStatement
                ? CreateConstructorBuilder(paramTypes, funcDeclaration)
                : CreateMethodBuilder(paramTypes, returnType, funcDeclaration);

            _localManager = new LocalManager(_funcScope, _il, returnType);

            return methodBase;
        }

        private MethodBase CreateConstructorBuilder(Type[] paramTypes, FuncDeclarationStatement funcDeclaration)
        {
            ConstructorBuilder builder = _typeBuilder.DefineConstructor(_methodAttributes,CallingConventions.HasThis, paramTypes);

            for (int i = 0; i < funcDeclaration.Params.Count; i++)
                builder.DefineParameter(i + 1, ParameterAttributes.None, funcDeclaration.Params[i].Name.Lexeme);

            _il = builder.GetILGenerator();

            // Call object ctor
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Call, typeof(Object).GetConstructor(Type.EmptyTypes));

            return builder;
        }

        private MethodBase CreateMethodBuilder(Type[] paramTypes, Type returnType, FuncDeclarationStatement funcDeclaration)
        {
            MethodBuilder methodBuilder = _typeBuilder.DefineMethod(funcDeclaration.Name.Lexeme, _methodAttributes, returnType, paramTypes);

            if (funcDeclaration.Name.Lexeme == "main")
                _assemblyBuilder.SetEntryPoint(methodBuilder);

            for (int i = 0; i < funcDeclaration.Params.Count; i++)
                methodBuilder.DefineParameter(i + 1, ParameterAttributes.None, funcDeclaration.Params[i].Name.Lexeme);

            _il = methodBuilder.GetILGenerator();

            return methodBuilder;
        }

        public void Assemble()
        {
            _scopeCrawler.Reset();
        
            AssembleStatement(_funcScope.FuncDeclaration.Body, _typeBuilder);

            if(_funcScope.FuncDeclaration is InitDeclarationStatement)
                _il.Emit(OpCodes.Ret);
        }

        private void AssembleStatement(Statement statement, TypeBuilder typeBuilder)
        {
            switch (statement)
            {
                case BlockStatement block:
                    _scopeCrawler.EnterScope();
                    foreach (Statement stmt in block.Body)
                        AssembleStatement(stmt, typeBuilder);
                    _scopeCrawler.ExitScope();
                    break;
                case ExpressionStatement exprStmt:
                    exprStmt.Expression.Accept(this, new ParentExpressionAssemblyContext());
                    break;
                case VarDeclarationStatement varDeclaration:
                    CobraType varType = CurrentScope.GetVarType(varDeclaration.Name.Lexeme);
                    _localManager.DeclareVar(CurrentScope, varDeclaration.Name.Lexeme, _typeStore.GetType(varType));
                    varDeclaration.Assignment?.Accept(this, new ParentExpressionAssemblyContext());
                    break;
                case ReturnStatement returnStatement:
                    returnStatement.Value.Accept(this, new ParentExpressionAssemblyContext());
                    ReturnStatement();
                    break;
                case IfStatement ifStatement:
                {
                    ifStatement.Condition.Accept(this, new ParentExpressionAssemblyContext());

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
                    _il.Emit(OpCodes.Nop);
                    break;
                }   
                case WhileStatement whileStatement:
                {
                    Label elseLabel = _il.DefineLabel();
                    Label endElseLabel = _il.DefineLabel();
                    Label whileLabel = _il.DefineLabel();
                    Label bodyLabel = _il.DefineLabel();

                    whileStatement.Condition.Accept(this, new ParentExpressionAssemblyContext());

                    _il.Emit(OpCodes.Brfalse, elseLabel); // If condition fails the first time go to else
                    _il.Emit(OpCodes.Br, bodyLabel); // Else go to body
                    _il.MarkLabel(whileLabel); // Return here on subsequent loop
                    whileStatement.Condition.Accept(this, new ParentExpressionAssemblyContext());
                    _il.Emit(OpCodes.Brfalse, endElseLabel);
                    _il.MarkLabel(bodyLabel); // Beginning of body
                    AssembleStatement(whileStatement.Then, typeBuilder);
                    _il.Emit(OpCodes.Br, whileLabel); // Return to condition
                    _il.MarkLabel(elseLabel);
                    if (whileStatement.Else != null)
                    {
                        AssembleStatement(whileStatement.Else, typeBuilder);
                    }
                    _il.MarkLabel(endElseLabel);
                    _il.Emit(OpCodes.Nop);
                    break;
                }
                default:
                    throw new NotImplementedException();
            }
        }

        private void ReturnStatement()
        {
            if (_funcScope.ReturnType != DotNetCobraType.Unit)
            {
                _localManager.StoreVar(CurrentScope, "@ret");
                _localManager.LoadVar(CurrentScope, "@ret");
            }
            
            _il.Emit(OpCodes.Ret);
        }


        public ExpressionAssemblyContext Visit(AssignExpression expr, ParentExpressionAssemblyContext context)
        {
            ExpressionAssemblyContext targetContext = expr.Target.Accept(this, new ParentExpressionAssemblyContext(assigning: true));
            ExpressionAssemblyContext valContext = expr.Value.Accept(this, new ParentExpressionAssemblyContext(expected:targetContext.Type));

            if(expr.Target is VarExpression varExpr)
                _localManager.StoreVar(CurrentScope, varExpr.Name.Lexeme);

            if (targetContext.AssigningToField)
                _il.Emit(OpCodes.Stfld, targetContext.AssignToField);

            return new ExpressionAssemblyContext(targetContext.Type);
        }

        public ExpressionAssemblyContext Visit(BinaryExpression expr, ParentExpressionAssemblyContext context)
        {
            CobraType leftType = expr.Left.Accept(this, new ParentExpressionAssemblyContext()).Type;
            CobraType rightType = expr.Right.Accept(this, new ParentExpressionAssemblyContext()).Type;

            IOperator op = _funcScope.GetOperator(Operator.GetOperation(expr.Op.Type), leftType, rightType);
            if (op is DotNetBinaryOperator dotNetBinaryOperator)
            {
                _il.Emit(dotNetBinaryOperator.OpCode);
            }

            return new ExpressionAssemblyContext(op.ResultType);
        }

        public ExpressionAssemblyContext Visit(CallExpression expr, ParentExpressionAssemblyContext context)
        {
            //Used for overload resolution TODO: improve performance
            List<CobraType> sig = new List<CobraType>();
            foreach (Expression arg in expr.Arguments)
                sig.Add(TypeChecker.GetExpressionType(arg, CurrentScope));

            sig.Add(TypeChecker.GetExpressionType(expr, CurrentScope));

            CobraGenericInstance expected = DotNetCobraGeneric.FuncType.CreateGenericInstance(sig);

            ExpressionAssemblyContext calleeContext = expr.Callee.Accept(this, new ParentExpressionAssemblyContext(calling: true, expected: expected));
            for (int i = 0; i < expr.Arguments.Count; i++)
                expr.Arguments[i].Accept(this, new ParentExpressionAssemblyContext(expected: sig[i]));
            
            CobraType returnType = null;

            if (calleeContext is MethodBuilderExpressionAssemblyContext methodExpressionContext)
            {
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
                _il.Emit(OpCodes.Callvirt, _typeStore.GetType(calleeContext.Type).GetMethod("Invoke") ?? throw new InvalidOperationException());
            }
            

            if (calleeContext.Type is CobraGenericInstance funcInstance)
                returnType = funcInstance.TypeParams.Last();

            return new ExpressionAssemblyContext(returnType);
        }

        public ExpressionAssemblyContext Visit(IndexExpression expr, ParentExpressionAssemblyContext arg)
        {
            ExpressionAssemblyContext collectionContext = expr.Collection.Accept(this, new ParentExpressionAssemblyContext());
            List<CobraType> indexTypes = new List<CobraType>();

            foreach (Expression index in expr.Indicies)
            {
                indexTypes.Add(index.Accept(this, new ParentExpressionAssemblyContext()).Type);
            }

            BinaryOperator getOp = CurrentScope.GetGenericBinaryOperator(Operation.Get, collectionContext.Type, DotNetCobraType.Int) ?? throw new NotImplementedException();

            MethodInfo get = _methodStore.GetMethodInfo(getOp) as MethodInfo;

            if (collectionContext.Type is CobraGenericInstance genericCollection)
            {
                get = get.DeclaringType.MakeGenericType(genericCollection.TypeParams.Select(_typeStore.GetType).ToArray()).GetMethod(get.Name,
                    get.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
            }

            _il.Emit(OpCodes.Callvirt, get);

            CobraType returnType = CurrentScope.GetOperator(Operation.Get, collectionContext.Type, DotNetCobraType.Int).ResultType;

            return new ExpressionAssemblyContext(returnType);
        }

        public ExpressionAssemblyContext Visit(ListLiteralExpression expr, ParentExpressionAssemblyContext context)
        {
            CobraType type = context.ExpectedType;

            Type listType = _typeStore.GetType(type);

            CobraType elementType = (type as CobraGenericInstance).TypeParams[0];
            ConstructorInfo listCtor;
            MethodInfo addMethod;

            if (listType.GetGenericArguments()[0] is TypeBuilder)
            {
                listCtor = TypeBuilder.GetConstructor(listType, listType.GetGenericTypeDefinition().GetConstructor(new[] {typeof(int)}) ?? throw new InvalidOperationException());
                addMethod = TypeBuilder.GetMethod(listType, listType.GetGenericTypeDefinition().GetMethod("Add") ?? throw new InvalidOperationException());
            }
            else
            {
                listCtor = listType.GetConstructor(new[] { typeof(int) }) ?? throw new InvalidOperationException();
                addMethod = listType.GetMethod("Add") ?? throw new InvalidOperationException();
            }
                
            _localManager.LoadLiteral(expr.Elements.Count);
            _il.Emit(OpCodes.Newobj, listCtor);



            foreach (Expression element in expr.Elements)
            {
                _il.Emit(OpCodes.Dup);
                element.Accept(this, new ParentExpressionAssemblyContext(expected:elementType));
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
            CobraType operandType = expr.Right.Accept(this, new ParentExpressionAssemblyContext()).Type;

            IOperator op = _funcScope.GetOperator(Operator.GetOperation(expr.Op.Type), null, operandType);
            if (op is DotNetBinaryOperator dotNetBinaryOperator)
            {
                _il.Emit(dotNetBinaryOperator.OpCode);
            }

            return new ExpressionAssemblyContext(op.ResultType);
        }

        public ExpressionAssemblyContext Visit(GroupingExpression expr, ParentExpressionAssemblyContext context)
        {
            return expr.Inner.Accept(this, new ParentExpressionAssemblyContext());
        }

        public ExpressionAssemblyContext Visit(VarExpression expr, ParentExpressionAssemblyContext context)
        {

            if (_methodStore.HasMethodBuilder(expr.Name.Lexeme))
            {
                CobraType varType = CurrentScope.GetVarType(expr.Name.Lexeme);
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
                _il.Emit(OpCodes.Newobj, _typeStore.GetType(mbContext.Type).GetConstructor(types) ?? throw new InvalidOperationException());
            }
            else if(!context.Assigning)
                _localManager.LoadVar(_scopeCrawler.Current, expr.Name.Lexeme);

            return new ExpressionAssemblyContext(CurrentScope.GetVarType(expr.Name.Lexeme));
        }

        public ExpressionAssemblyContext Visit(GetExpression expr, ParentExpressionAssemblyContext context)
        {
            ExpressionAssemblyContext exprContext = expr.Obj.Accept(this, new ParentExpressionAssemblyContext());
            if (exprContext.Type is NamespaceType _namespace)
            {
                if(_namespace.HasType(expr.Name.Lexeme))
                    return new ExpressionAssemblyContext(_namespace.GetType(expr.Name.Lexeme));

                Token resolvedToken = new Token(TokenType.Identifier, _namespace.ResolveName(expr.Name.Lexeme), null,
                    expr.Name.Line);

                return Visit(new VarExpression(resolvedToken), context);
            }

            //TODO implement class method overloading
            Type varType = _typeStore.GetType(exprContext.Type);
            MemberInfo member = _typeStore.GetMemberInfo(exprContext.Type, expr.Name.Lexeme, context.ExpectedType);

            switch (member)
            {
                case PropertyInfo prop:
                    MethodInfo get = prop.GetGetMethod();
                    _il.Emit(OpCodes.Callvirt, get);
                    break;
                case FieldInfo field:
                    if(!context.Assigning)
                        _il.Emit(OpCodes.Ldfld, field);
                    else 
                        return new ExpressionAssemblyContext(exprContext.Type.GetSymbol(expr.Name.Lexeme), field);
                    break;
                case MethodInfo method:
                    if (context.ImmediatelyCalling)
                        return new MethodBuilderExpressionAssemblyContext(exprContext.Type.GetSymbol(expr.Name.Lexeme), method);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return new ExpressionAssemblyContext(exprContext.Type.GetSymbol(expr.Name.Lexeme));
        }
    }
}
