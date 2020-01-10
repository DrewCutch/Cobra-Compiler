using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Parse.TypeCheck;
using CobraCompiler.Parse.TypeCheck.Operators;
using CobraCompiler.Parse.TypeCheck.Types;

namespace CobraCompiler.Assemble
{
    class Assembler
    {
        public readonly string AssemblyName;
        public string AssemblyFileName => $"{AssemblyName}.exe";

        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly TypeStore _typeStore;
        private readonly Dictionary<Scope, Dictionary<string, LocalBuilder>> _locals;

        private const TypeAttributes ModuleTypeAttributes =
            TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed;

        private const MethodAttributes FuncMethodAttributes = MethodAttributes.Public | MethodAttributes.Static;

        private readonly OpCode[] _loadArgShortCodes = new OpCode[] {
            OpCodes.Ldarg_0, OpCodes.Ldarg_1, OpCodes.Ldarg_2, OpCodes.Ldarg_3
        };

        private ILGenerator _il;
        private FuncScope _currentFunc;

        private Stack<Scope> _scopeStack;
        private Scope CurrentScope => _scopeStack.Peek();
        
        private MethodStore _methodStore;

        public Assembler(String assName)
        {
            AssemblyName = assName;

            AppDomain appDomain = AppDomain.CurrentDomain;
            AssemblyName assemblyName = new AssemblyName(AssemblyName);

            _assemblyBuilder = appDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Save);
            _typeStore = new TypeStore(_assemblyBuilder);
            _methodStore = new MethodStore(_assemblyBuilder);
            _locals = new Dictionary<Scope, Dictionary<string, LocalBuilder>>();
            _scopeStack = new Stack<Scope>();

            foreach (DotNetCobraType dotNetCobraType in DotNetCobraType.DotNetCobraTypes)
            {
                _typeStore.AddType(dotNetCobraType, dotNetCobraType.Type);
                _typeStore.AddTypeAlias(dotNetCobraType.Identifier, dotNetCobraType.Type);
            }
        }

        public void AssembleModule(ModuleScope scope)
        {
            ModuleBuilder moduleBuilder = _assemblyBuilder.DefineDynamicModule(AssemblyName, AssemblyFileName);
            TypeBuilder typeBuilder = moduleBuilder.DefineType(scope.Name, ModuleTypeAttributes);

            FuncAssemblerFactory funcAssemblerFactory = new FuncAssemblerFactory(_assemblyBuilder, typeBuilder, _typeStore, _methodStore);
            
            List<FuncAssembler> funcAssemblers = new List<FuncAssembler>();
            
            foreach (Scope subScope in scope.SubScopes)
            {
                if (subScope is FuncScope funcScope)
                {
                    FuncAssembler funcAssembler = funcAssemblerFactory.CreateFuncAssembler(funcScope);
                    MethodBuilder builder = funcAssembler.AssembleDefinition();
                    funcAssemblers.Add(funcAssembler);

                    _methodStore.AddMethodInfo(funcScope.FuncDeclaration.Name.Lexeme, funcScope.FuncType, builder);
                }
            }

            MethodInfo printStrInfo = typeof(Console).GetMethod("WriteLine", new[] {typeof(string)});
            MethodInfo printIntInfo = typeof(Console).GetMethod("WriteLine", new[] { typeof(int) });
            MethodInfo listGetInfo = typeof(List<>).GetMethod("get_Item");

            _methodStore.AddMethodInfo("printStr", DotNetCobraGeneric.FuncType.CreateGenericInstance(new[] { DotNetCobraType.Str, DotNetCobraType.Null }), printStrInfo);
            _methodStore.AddMethodInfo("printInt", DotNetCobraGeneric.FuncType.CreateGenericInstance(new[]{DotNetCobraType.Int, DotNetCobraType.Null}), printIntInfo);
            _methodStore.AddMethodInfo("get_Item", GenericOperator.DotNetGenericOperators[0].GetGenericFuncType(), listGetInfo);

            foreach (FuncAssembler assembler in funcAssemblers)
            {
                assembler.AssembleBody();
            }

            typeBuilder.CreateType();
        }

        public void SaveAssembly()
        {
            _assemblyBuilder.Save(AssemblyFileName);
        }

    }
}
