using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Scopes;

namespace CobraCompiler.Assemble
{
    class FuncAssemblerFactory
    {
        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly TypeBuilder _typeBuilder;
        private readonly TypeStore _typeStore;
        private readonly MethodStore _methodStore;

        public FuncAssemblerFactory(AssemblyBuilder assemblyBuilder, TypeBuilder typeBuilder, TypeStore typeStore, MethodStore methodStore)
        {
            _assemblyBuilder = assemblyBuilder;
            _typeBuilder = typeBuilder;
            _typeStore = typeStore;
            _methodStore = methodStore;
        }

        public FuncAssembler CreateFuncAssembler(FuncScope funcScope)
        {
            return new FuncAssembler(funcScope, _typeStore, _methodStore, _typeBuilder, _assemblyBuilder);
        }
    }
}
