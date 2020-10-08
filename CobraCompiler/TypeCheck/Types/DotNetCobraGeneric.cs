using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using CobraCompiler.TypeCheck.Symbols;

namespace CobraCompiler.TypeCheck.Types
{
    public delegate Type GenericInstanceGenerator(params Type[] typeParams);

    class DotNetCobraGeneric : CobraGeneric, ITypeGenerator
    {
        public static DotNetCobraGeneric FuncType = new FuncCobraGeneric();
        public static DotNetCobraGeneric ListType = new DotNetCobraGeneric("list", 1, typeof(List<>).MakeGenericType, typeof(List<>));
        
        public static CobraGeneric[] BuiltInCobraGenerics;

        static DotNetCobraGeneric()
        {
            var awake = FuncCobraGeneric.FuncType;

            BuiltInCobraGenerics = new CobraGeneric[]{
                FuncType,
                ListType,
                UnionLangCobraGeneric.UnionGeneric,
                IntersectionLangCobraGeneric.IntersectGeneric
            };
        }

        public readonly GenericInstanceGenerator InstanceGenerator;

        private Type _type;

        public DotNetCobraGeneric(string identifier, int numberOfParams, GenericInstanceGenerator instanceGenerator, Type type = null) : base(identifier, GenerateTypeParamPlaceholders(numberOfParams))
        {
            InstanceGenerator = instanceGenerator;
            _type = type;

            if (_type != null)
                DefineSymbols();
        }

        private void DefineSymbols()
        {
            PropertyInfo[] properties = _type.GetProperties();

            foreach (PropertyInfo property in properties)
            {
                Mutability propertyMutability = property.CanWrite ? Mutability.Mutable : Mutability.ReadOnly;

                DotNetCobraType propertyType = DotNetCobraType.FromType(property.PropertyType);
                if (propertyType != null)
                    DefineSymbol(property.Name, new Symbol(null, propertyType, propertyMutability, property.Name));
            }
        }

        public Type GetType(ModuleBuilder mb, params Type[] typeArgs)
        {
            return InstanceGenerator(typeArgs);
        }

        public override CobraGenericInstance CreateGenericInstance(IReadOnlyList<CobraType> typeParams)
        {
            CobraGenericInstance instance = base.CreateGenericInstance(typeParams);

            return instance;
        }
    }
}
