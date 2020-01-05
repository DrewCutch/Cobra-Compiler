using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Assemble.ExpressionAssemblyContexts;
using CobraCompiler.Parse.TypeCheck;

namespace CobraCompiler.Assemble
{
    class MethodStore
    {
        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly Dictionary<(string, CobraType), MethodInfo> _methodAlias;
        private readonly Dictionary<string, Dictionary<CobraType, MethodInfo>> _methodBuilders;
        

        public MethodStore(AssemblyBuilder assemblyBuilder)
        {
            _assemblyBuilder = assemblyBuilder;

            _methodAlias = new Dictionary<(string, CobraType), MethodInfo>();
            _methodBuilders = new Dictionary<string, Dictionary<CobraType, MethodInfo>>();
        }

        public bool HasMethodBuilder(string identifier)
        {
            return _methodBuilders.ContainsKey(identifier);
        }

        public void AddMethodInfo(string identifier, CobraType funcType, MethodInfo info)
        {
            if(!_methodBuilders.ContainsKey(identifier))
                _methodBuilders[identifier] = new Dictionary<CobraType, MethodInfo>();

            _methodBuilders[identifier][funcType] = info;
        }

        public MethodInfo GetMethodInfo(MethodExpressionAssemblyContext context)
        {
            switch (context)
            {
                case MethodBuilderExpressionAssemblyContext methodBuilderContext:
                    return _methodBuilders[methodBuilderContext.Identifier][context.Type];
            }

            throw new NotImplementedException();
        }
    }
}
