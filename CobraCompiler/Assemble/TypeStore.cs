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
        private readonly ModuleBuilder _moduleBuilder;
        private readonly Dictionary<string, Type> _typeAlias;
        private readonly Dictionary<CobraType, Type> _types;
        private readonly Dictionary<CobraType, Dictionary<string, List<MemberInfo>>> _typeMembers;
        private readonly Dictionary<string, GenericTypeAssembler> _genericInstanceGenerators;

        public TypeStore(AssemblyBuilder assemblyBuilder, ModuleBuilder moduleBuilder)
        {
            _assemblyBuilder = assemblyBuilder;
            _moduleBuilder = moduleBuilder;
            _typeAlias = new Dictionary<string, Type>();
            _types = new Dictionary<CobraType, Type>();
            _typeMembers = new Dictionary<CobraType, Dictionary<string, List<MemberInfo>>>();
            _genericInstanceGenerators = new Dictionary<string, GenericTypeAssembler>();
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
            if (cobraType is CobraGenericInstance genericInstance && genericInstance.Base is ITypeGenerator typeGen)
                return typeGen.GetType(_moduleBuilder, genericInstance.TypeParams.Select(GetType).ToArray());

            return _types[cobraType];
        }

        private Type GetType(TypeInitExpression typeInit)
        {
            if(!typeInit.IsGenericInstance)
                return GetType(typeInit.IdentifierStr);

            string genericName = typeInit.IdentifierStrWithoutParams;

            if (!_genericInstanceGenerators.ContainsKey(genericName))
                return null;

            return _genericInstanceGenerators[genericName](_moduleBuilder, typeInit.GenericParams.Select(GetType).ToArray());
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

        public void AddTypeMember(CobraType type, MemberInfo member)
        {
            AddTypeMember(type, member.Name, member);
        }

        public void AddTypeMember(CobraType type, string memberName, MemberInfo member)
        {
            if(!_typeMembers.ContainsKey(type))
                _typeMembers[type] = new Dictionary<string, List<MemberInfo>>();

            if(!_typeMembers[type].ContainsKey(memberName))
                _typeMembers[type][memberName] = new List<MemberInfo>();

            _typeMembers[type][memberName].Add(member);
        }

        public MemberInfo[] GetMemberInfo(CobraType cobraType, string memberName)
        {
            Type type = GetType(cobraType);

            if (!(type is TypeBuilder))
                return type.GetMember(memberName);

            List<MemberInfo> members = new List<MemberInfo>();
            if(_typeMembers.ContainsKey(cobraType) && _typeMembers[cobraType].ContainsKey(memberName))
                members.AddRange(_typeMembers[cobraType][memberName].ToArray());

            foreach (CobraType parent in cobraType.Parents)
                members.AddRange(GetMemberInfo(parent, memberName));

            return members.ToArray();
        }
    }
}
