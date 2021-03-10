using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.Assemble
{
    class TypeStore
    {
        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly ModuleBuilder _moduleBuilder;
        private readonly Dictionary<string, Type> _typeAlias;
        private readonly Dictionary<CobraType, Type> _types;
        private readonly Dictionary<CobraType, GenericTypeParameterBuilder> _currentGenerics;
        private readonly Dictionary<CobraType, Dictionary<string, Dictionary<CobraType, MemberInfo>>> _typeMembers;
        private readonly Dictionary<string, GenericTypeAssembler> _genericInstanceGenerators;

        public TypeStore(AssemblyBuilder assemblyBuilder, ModuleBuilder moduleBuilder)
        {
            _assemblyBuilder = assemblyBuilder;
            _moduleBuilder = moduleBuilder;
            _typeAlias = new Dictionary<string, Type>();
            _types = new Dictionary<CobraType, Type>();
            _currentGenerics = new Dictionary<CobraType, GenericTypeParameterBuilder>();
            _typeMembers = new Dictionary<CobraType, Dictionary<string, Dictionary<CobraType, MemberInfo>>>();
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

        public void UpdateType(CobraType cobraType, Type type)
        {
            if(!_types.ContainsKey(cobraType))
                throw new ArgumentException("Cannot update type not yet added to type store!", nameof(cobraType));

            _types[cobraType] = type;
        }

        public void PushCurrentGenerics(Dictionary<CobraType, GenericTypeParameterBuilder> generics)
        {
            generics.ToList().ForEach(x => _currentGenerics.Add(x.Key, x.Value));
        }

        public void PopGenerics(Dictionary<CobraType, GenericTypeParameterBuilder> generics)
        {
            generics.ToList().ForEach(x => _currentGenerics.Remove(x.Key));
        }

        public void ClearCurrentGenerics()
        {
            _currentGenerics.Clear();
        }

        public Type GetType(CobraType cobraType)
        {
            if (cobraType.IsNullable)
                cobraType = cobraType.NullableBase;

            if(cobraType.IsTypeParamPlaceholder)
                return _currentGenerics[cobraType];

            if (cobraType.IsConstructedGeneric && cobraType.GenericBase is ITypeGenerator typeGen)
                return typeGen.GetType(_moduleBuilder, cobraType.OrderedTypeArguments.Select(GetType).ToArray());

            if (cobraType.IsConstructedGeneric)
            {
                foreach (CobraType key in _types.Keys)
                {
                    if(!(key.IsConstructedGeneric) || key.GenericBase != cobraType.GenericBase)
                        continue;

                    Type Generic = _types[key];

                    return Generic.MakeGenericType(cobraType.OrderedTypeArguments.Select(GetType).ToArray());
                }

                return _types[cobraType.GenericBase].MakeGenericType(cobraType.OrderedTypeArguments.Select(GetType).ToArray());
            }

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

        public void AddTypeMember(CobraType type, CobraType memberType, MemberInfo member)
        {
            AddTypeMember(type, member.Name, memberType, member);
        }

        public void AddTypeMember(CobraType type, string memberName, CobraType memberType, MemberInfo member)
        {
            if(!_typeMembers.ContainsKey(type))
                _typeMembers[type] = new Dictionary<string, Dictionary<CobraType, MemberInfo>>();

            if(!_typeMembers[type].ContainsKey(memberName))
                _typeMembers[type][memberName] = new Dictionary<CobraType, MemberInfo>();

            _typeMembers[type][memberName][memberType] = member;
        }

        public bool TypeMemberExists(CobraType cobraType, string memberName, CobraType memberType) =>
            GetMemberInfo(cobraType, memberName, memberType) != null;

        public MemberInfo GetMemberInfo(CobraType cobraType, string memberName, CobraType memberType)
        {
            if (cobraType.IsNullable)
                cobraType = cobraType.NullableBase;

            Type type = GetType(cobraType);

            if (type.ContainsGenericParameters && type.Module != _moduleBuilder)
                return type.GetGenericTypeDefinition().GetMember(memberName).FirstOrDefault();

            if (type.Module != _moduleBuilder && type.IsConstructedGenericType)
                return type.GetGenericTypeDefinition().GetMember(memberName).FirstOrDefault();

            if (type.Module != _moduleBuilder)
                return type.GetMember(memberName).FirstOrDefault();


            if (type.IsConstructedGenericType && cobraType.IsConstructedGeneric)
            {
                MemberInfo genericInfo = _typeMembers[cobraType.GenericBase][memberName].Values.FirstOrDefault();
                TypeBuilder baseType = genericInfo.DeclaringType as TypeBuilder;
                MemberInfo[] members = baseType.GetMembers();
                
                //TODO: this is probably wrong
                return genericInfo;
            }

            if (!(memberType is FuncGenericInstance func))
            {
                if (!_typeMembers[cobraType].ContainsKey(memberName))
                    return null;
                
                return _typeMembers[cobraType][memberName].Values.FirstOrDefault();
            }

            foreach (CobraType key in _typeMembers[cobraType][memberName].Keys)
            {
                FuncGenericInstance keyFunc = key as FuncGenericInstance;

                if(keyFunc.TypeArguments.Count != func.TypeArguments.Count)
                    continue;

                bool matches = true;

                for (int i = 0; i < func.TypeParams.Count; i++)
                    if (!func.OrderedTypeArguments[i].CanCastTo(keyFunc.OrderedTypeArguments[i]))
                    {
                        matches = false;
                        break;
                    }

                if (!matches)
                    continue;

                return _typeMembers[cobraType][memberName][keyFunc];
            }

            return null;
        }
    }
}
