using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Assemble
{
    class TypeStore
    {
        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly Dictionary<string, Type> _typeAlias;

        public TypeStore(AssemblyBuilder assemblyBuilder)
        {
            _assemblyBuilder = assemblyBuilder;
            _typeAlias = new Dictionary<string, Type>();
        }

        public void AddTypeAlias(string identifier, Type type)
        {
            _typeAlias[identifier] = type;
        }

        public Type GetType(string identifier)
        {
            if (_typeAlias.TryGetValue(identifier, out Type aliasedType))
                return aliasedType;

            if (_assemblyBuilder.GetType(identifier) is Type assemblyType)
                return assemblyType;

            if (Type.GetType(identifier) is Type coreType)
                return coreType;

            return null;
        }
    }
}
