using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Compiler;
using CobraCompiler.Parse;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;
using CobraCompiler.TypeCheck.Operators;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.Assemble
{
    class Assembler
    {
        public readonly string AssemblyName;
        public string AssemblyFileName => $"{AssemblyName}.exe";

        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly ModuleBuilder _moduleBuilder;
        private readonly TypeStore _typeStore;
        private readonly Dictionary<Scope, Dictionary<string, LocalBuilder>> _locals;

        private const TypeAttributes ModuleTypeAttributes =
            TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed;

        private const MethodAttributes FuncMethodAttributes = MethodAttributes.Public | MethodAttributes.Static;

        private readonly OpCode[] _loadArgShortCodes = new OpCode[] {
            OpCodes.Ldarg_0, OpCodes.Ldarg_1, OpCodes.Ldarg_2, OpCodes.Ldarg_3
        };

        private readonly MethodStore _methodStore;

        public Assembler(String assName)
        {
            AssemblyName = assName;

            AppDomain appDomain = AppDomain.CurrentDomain;
            AssemblyName assemblyName = new AssemblyName(AssemblyName);

            _assemblyBuilder = appDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Save);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(assName, AssemblyFileName);

            _typeStore = new TypeStore(_assemblyBuilder, _moduleBuilder);
            _methodStore = new MethodStore(_assemblyBuilder);
            _locals = new Dictionary<Scope, Dictionary<string, LocalBuilder>>();

            foreach (DotNetCobraType dotNetCobraType in DotNetCobraType.DotNetCobraTypes)
            {
                _typeStore.AddType(dotNetCobraType, dotNetCobraType.Type);
                _typeStore.AddTypeAlias(dotNetCobraType.Identifier, dotNetCobraType.Type);
            }
        }

        public void AssembleProject(CheckedProject project)
        {
            List<DefinedModule> definedModules = new List<DefinedModule>();

            foreach (Scope subScope in project.Scope.SubScopes)
            {
                if (subScope is ModuleScope module)
                    definedModules.Add(CreateModule(module, _moduleBuilder));
            }

            MethodInfo printStrInfo = typeof(Console).GetMethod("WriteLine", new[] { typeof(string) });
            MethodInfo printIntInfo = typeof(Console).GetMethod("WriteLine", new[] { typeof(int) });

            _methodStore.AddMethodInfo("printStr", DotNetCobraGeneric.FuncType.CreateGenericInstance(DotNetCobraType.Str, DotNetCobraType.Unit ), printStrInfo);
            _methodStore.AddMethodInfo("printInt", DotNetCobraGeneric.FuncType.CreateGenericInstance(DotNetCobraType.Int, DotNetCobraType.Unit ), printIntInfo);
            
            foreach (DefinedModule module in definedModules)
            {
                foreach (IAssemble assembler in module.ToAssemble)
                    assembler.Assemble();

                module.TypeBuilder.CreateType();
            }
        }

        public DefinedModule CreateModule(ModuleScope scope, ModuleBuilder mb)
        {
            TypeBuilder typeBuilder = mb.DefineType(scope.Name, ModuleTypeAttributes);

            FuncAssemblerFactory funcAssemblerFactory = new FuncAssemblerFactory(_assemblyBuilder, typeBuilder, _typeStore, _methodStore, FuncMethodAttributes);

            List<IAssemble> assemblers = new List<IAssemble>();

            List<InterfaceAssembler> typeAssemblers = new List<InterfaceAssembler>();
            foreach (CobraType definedType in scope.DefinedTypes)
            {
                if ((definedType.IsGenericType && definedType.HasFixedParamCount) ||
                    !(definedType.IsConstructedGeneric))
                {
                    InterfaceAssembler typeAssembler = new InterfaceAssembler(scope.Name, definedType, _typeStore, mb);
                    typeAssembler.AssembleDefinition();

                    typeAssemblers.Add(typeAssembler);
                }
            }

            foreach (InterfaceAssembler assembler in typeAssemblers)
                assembler.Assemble();

            Queue<Scope> subScopes = new Queue<Scope>(scope.SubScopes);

            while (subScopes.Count > 0)
            {
                Scope subScope = subScopes.Dequeue();

                switch (subScope)
                {
                    case FuncScope funcScope:
                    {
                        FuncAssembler funcAssembler = funcAssemblerFactory.CreateFuncAssembler(funcScope);
                        MethodBase builder = funcAssembler.AssembleDefinition();
                        assemblers.Add(funcAssembler);
                    
                        _methodStore.AddMethodInfo(scope.Name + "." + funcScope.FuncDeclaration.Name.Lexeme, funcScope.FuncType, builder);
                        _methodStore.AddMethodInfo(funcScope.FuncDeclaration.Name.Lexeme, funcScope.FuncType, builder);
                        break;
                    }
                    case ClassScope classScope:
                    {
                        ClassAssembler classAssembler = new ClassAssembler(classScope, _typeStore, _methodStore, _assemblyBuilder, _moduleBuilder);
                        TypeBuilder builder = classAssembler.AssembleDefinition();

                        _typeStore.AddType(classScope.ThisType, builder);
                        assemblers.Add(classAssembler);
                        break;
                    }
                    case Scope plainScope:
                        foreach (Scope scopeSubScope in plainScope.SubScopes)
                            subScopes.Enqueue(scopeSubScope);
                        break;
                }
            }
            
            return new DefinedModule(assemblers, typeBuilder);
        }

        public void SaveAssembly()
        {
            _assemblyBuilder.Save(AssemblyFileName);
        }

    }
}
