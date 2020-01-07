using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.TypeCheck;
using CobraCompiler.Parse.TypeCheck.Types;

namespace CobraCompiler.Assemble
{
    class TypeStore
    {
        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly Dictionary<string, Type> _typeAlias;
        public readonly Dictionary<CobraType, Type> _types;
        private readonly Dictionary<string, GenericInstanceGenerator> _genericInstanceGenerators;

        public TypeStore(AssemblyBuilder assemblyBuilder)
        {
            _assemblyBuilder = assemblyBuilder;
            _typeAlias = new Dictionary<string, Type>();
            _types = new Dictionary<CobraType, Type>();
            _genericInstanceGenerators = new Dictionary<string, GenericInstanceGenerator>();
        }

        public void AddTypeAlias(string identifier, Type type)
        {
            _typeAlias[identifier] = type;
        }

        public void AddType(CobraType cobraType, Type type)
        {
            _types[cobraType] = type;
        }

        public Type GetType(CobraType cobraType)
        {
            if (cobraType is CobraGenericInstance genericInstance && genericInstance.Base is DotNetCobraGeneric generic)
                return generic.GetType(genericInstance.TypeParams.Select(GetType).ToArray());

            return _types[cobraType];
        }

        private Type GetType(TypeInitExpression typeInit)
        {
            if(!typeInit.IsGenericInstance)
                return GetType(typeInit.IdentifierStr);

            string genericName = typeInit.IdentifierStrWithoutParams;

            if (!_genericInstanceGenerators.ContainsKey(genericName))
                return null;

            return _genericInstanceGenerators[genericName](typeInit.GenericParams.Select(GetType).ToArray());
        }

        private Type GetType(string identifier)
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
