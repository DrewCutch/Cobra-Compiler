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
using CobraCompiler.Parse.TypeCheck;
using CobraCompiler.Parse.TypeCheck.Operators;
using CobraCompiler.Parse.TypeCheck.Types;
using CobraCompiler.Scanning;
using InvalidOperationException = System.InvalidOperationException;

namespace CobraCompiler.Assemble
{
    class FuncAssembler: IExpressionVisitorWithContext<ExpressionAssemblyContext, ParentExpressionAssemblyContext>
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

            Type[] paramTypes = _funcScope.Params.Select(param => _typeStore.GetType(param.Item2)).ToArray();
            Type returnType = _funcScope.ReturnType != null ? _typeStore.GetType(_funcScope.ReturnType) : null;

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
        
            AssembleStatement(_funcScope.FuncDeclaration.Body, _typeBuilder);
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
            CobraType expectedType = CurrentScope.GetVarType(expr.Name.Lexeme);

            ExpressionAssemblyContext valContext = expr.Value.Accept(this, new ParentExpressionAssemblyContext(expected:expectedType));

            _localManager.StoreVar(_scopeCrawler.Current, expr.Name.Lexeme);

            return new ExpressionAssemblyContext(_funcScope.GetVarType(expr.Name.Lexeme));
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
            ExpressionAssemblyContext calleeContext = expr.Callee.Accept(this, new ParentExpressionAssemblyContext(calling:true));

            foreach (Expression arg in expr.Arguments)
                arg.Accept(this, new ParentExpressionAssemblyContext());

            CobraType returnType = null;

            if (calleeContext is MethodExpressionAssemblyContext methodExpressionContext)
            {
                MethodInfo method = _methodStore.GetMethodInfo(methodExpressionContext);

                _il.Emit(OpCodes.Call, method);
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

            MethodInfo get = _methodStore.GetMethodInfo(getOp);

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
                MethodBuilderExpressionAssemblyContext mbContext =
                    new MethodBuilderExpressionAssemblyContext(CurrentScope.GetVarType(expr.Name.Lexeme), expr.Name.Lexeme);

                if (context.ImmediatelyCalling)
                    return mbContext;
                
                _il.Emit(OpCodes.Ldnull);
                _il.Emit(OpCodes.Ldftn, _methodStore.GetMethodInfo(mbContext));

                Type[] types = { typeof(object), typeof(IntPtr) };
                _il.Emit(OpCodes.Newobj, _typeStore.GetType(mbContext.Type).GetConstructor(types) ?? throw new InvalidOperationException());
            }
            else
                _localManager.LoadVar(_scopeCrawler.Current, expr.Name.Lexeme);

            return new ExpressionAssemblyContext(CurrentScope.GetVarType(expr.Name.Lexeme));
        }

        public ExpressionAssemblyContext Visit(GetExpression expr, ParentExpressionAssemblyContext context)
        {
            ExpressionAssemblyContext exprContext = expr.Obj.Accept(this, context);
            if (exprContext.Type is NamespaceType _namespace)
            {
                if(_namespace.HasType(expr.Name.Lexeme))
                    return new ExpressionAssemblyContext(_namespace.GetType(expr.Name.Lexeme));

                Token resolvedToken = new Token(TokenType.Identifier, _namespace.ResolveName(expr.Name.Lexeme), null,
                    expr.Name.Line);

                return Visit(new VarExpression(resolvedToken), context);
            }

            Type varType = _typeStore.GetType(exprContext.Type);
            MemberInfo[] members = varType.GetMember(expr.Name.Lexeme);

            if(members.Length > 1)
                throw new NotImplementedException();

            MemberInfo member = members[0];

            switch (member)
            {
                case PropertyInfo prop:
                    MethodInfo get = prop.GetGetMethod();
                    _il.Emit(OpCodes.Callvirt, get);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return new ExpressionAssemblyContext(exprContext.Type.GetSymbol(expr.Name.Lexeme));

            throw new NotImplementedException();
        }
    }
}
