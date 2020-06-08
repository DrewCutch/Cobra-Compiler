using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Assemble.ExpressionAssemblyContexts;
using CobraCompiler.TypeCheck.Operators;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.Assemble
{
    class MethodStore
    {
        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly Dictionary<(string, CobraType), MethodInfo> _methodAlias;
        private readonly Dictionary<string, Dictionary<CobraType, MethodBase>> _methodBuilders;

        public MethodStore(AssemblyBuilder assemblyBuilder)
        {
            _assemblyBuilder = assemblyBuilder;

            _methodAlias = new Dictionary<(string, CobraType), MethodInfo>();
            _methodBuilders = new Dictionary<string, Dictionary<CobraType, MethodBase>>();
        }

        public bool HasMethodBuilder(string identifier)
        {
            return _methodBuilders.ContainsKey(identifier);
        }

        public void AddMethodInfo(string identifier, CobraType funcType, MethodBase info)
        {
            if(!_methodBuilders.ContainsKey(identifier))
                _methodBuilders[identifier] = new Dictionary<CobraType, MethodBase>();

            _methodBuilders[identifier][funcType] = info;
        }

        public MethodBase GetMethodInfo(BinaryOperator op)
        {
            string methodName = Operator.GetOverloadSpecialName(op.Operation);
            CobraType methodType = op.GetFuncType();

            return _methodBuilders[methodName][methodType];
        }

        public MethodBase GetMethodInfo(CobraType type, string identifier)
        {
            return _methodBuilders[identifier][type];
        }
    }
}
