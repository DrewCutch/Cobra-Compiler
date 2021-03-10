using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using CobraCompiler.TypeCheck.Symbols;
using CobraCompiler.Util;

namespace CobraCompiler.TypeCheck.Types
{
    public delegate Type GenericInstanceGenerator(params Type[] typeParams);

    class DotNetCobraGeneric : CobraType, ITypeGenerator
    {
        public static DotNetCobraGeneric FuncType = new FuncCobraGeneric();
        public static DotNetCobraGeneric ListType = new DotNetCobraGeneric("list", 1, typeof(List<>).MakeGenericType, typeof(List<>));
        
        public static CobraType[] BuiltInCobraGenerics;

        static DotNetCobraGeneric()
        {
            var awake = FuncCobraGeneric.FuncType;

            BuiltInCobraGenerics = new CobraType[]{
                FuncType,
                ListType,
                UnionLangCobraGeneric.UnionGeneric,
                IntersectionLangCobraGeneric.IntersectGeneric
            };
        }

        public readonly GenericInstanceGenerator InstanceGenerator;

        private Type _type;

        public DotNetCobraGeneric(string identifier, int numberOfParams, GenericInstanceGenerator instanceGenerator, Type type = null) :
            base(identifier, new List<CobraType>(),  true, GenerateTypeParamPlaceholders(numberOfParams), new List<CobraType>(), null, -1)
        {
            InstanceGenerator = instanceGenerator;
            _type = type;

            if (_type != null)
                DefineSymbols();
        }

        private void DefineSymbols()
        {
            Type[] typeArguments = _type.GetGenericArguments();
            Dictionary<Type, CobraType> typeArgs = new Dictionary<Type, CobraType>();
            
            foreach ((Type typeArgument, int i) in typeArguments.WithIndex())
            {
                typeArgs[typeArgument] = GenericPlaceholder(typeArgument.Name, i);
            }

            PropertyInfo[] properties = _type.GetProperties();

            foreach (PropertyInfo property in properties)
            {
                Mutability propertyMutability = property.CanWrite ? Mutability.Mutable : Mutability.ReadOnly;

                DotNetCobraType propertyType = DotNetCobraType.FromType(property.PropertyType);
                if (propertyType != null)
                    DefineSymbol(property.Name, new Symbol(null, propertyType, SymbolKind.Member, propertyMutability, property.Name));
            }

            MethodInfo[] methods = _type.GetMethods();

            foreach (MethodInfo method in methods)
            {
                ParameterInfo[] parameters = method.GetParameters();
                CobraType[] callSignature = new CobraType[parameters.Length + 1];

                foreach ((ParameterInfo parameter, int i) in parameters.WithIndex())
                {
                    CobraType parameterType = parameter.ParameterType.IsGenericParameter
                        ? typeArgs[parameter.ParameterType]
                        : DotNetCobraType.FromType(parameter.ParameterType);

                    if(parameterType == null)
                        goto nextMethod;

                    callSignature[i] = parameterType;
                }

                CobraType returnType = method.ReturnType.IsGenericParameter
                    ? typeArgs[method.ReturnType]
                    : DotNetCobraType.FromType(method.ReturnType);

                if(returnType == null)
                    goto nextMethod;

                callSignature[parameters.Length] = returnType;

                CobraType funcType = DotNetCobraGeneric.FuncType.CreateGenericInstance(callSignature);

                Symbol symbol = new Symbol(null, funcType, SymbolKind.Member, Mutability.ReadOnly, method.Name);

                DefineSymbol(method.Name, symbol, true);

                nextMethod: ;
            }
        }

        public Type GetType(ModuleBuilder mb, params Type[] typeArgs)
        {
            return InstanceGenerator(typeArgs);
        }

        public override CobraType CreateGenericInstance(IReadOnlyList<CobraType> typeParams)
        {
            CobraType instance = base.CreateGenericInstance(typeParams);

            return instance;
        }
    }
}
