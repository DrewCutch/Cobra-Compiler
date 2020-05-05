using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using CobraCompiler.Assemble.LangTypeAssemblers;

namespace CobraCompiler.Parse.TypeCheck.Types
{
    public delegate Type GenericInstanceGenerator(params Type[] typeParams);

    class DotNetCobraGeneric : CobraGeneric, ITypeGenerator
    {
        public static DotNetCobraGeneric FuncType = new DotNetCobraGeneric("f", -1, Expression.GetDelegateType);
        public static DotNetCobraGeneric ListType = new DotNetCobraGeneric("list", 1, typeof(List<>).MakeGenericType);

        public static CobraGeneric[] BuiltInCobraGenerics =
        {
            FuncType,
            ListType,
            UnionLangCobraGeneric.UnionGeneric,
            IntersectionLangCobraGeneric.IntersectGeneric
        };

        public readonly GenericInstanceGenerator InstanceGenerator;

        public DotNetCobraGeneric(string identifier, int numberOfParams, GenericInstanceGenerator instanceGenerator) : base(identifier, numberOfParams)
        {
            InstanceGenerator = instanceGenerator;
        }

        public Type GetType(ModuleBuilder mb, params Type[] typeArgs)
        {
            return InstanceGenerator(typeArgs);
        }

        public override CobraGenericInstance CreateGenericInstance(IReadOnlyList<CobraType> typeParams)
        {
            CobraGenericInstance instance = base.CreateGenericInstance(typeParams);

            Type[] typeArgs = new Type[typeParams.Count];
            for(int i = 0; i < typeParams.Count; i++)
            {
                if (typeParams[i] is DotNetCobraType dotNetType)
                    typeArgs[i] = dotNetType.Type;
                else
                    return instance;
            }

            Type instanceType = GetType(null, typeArgs);
            PropertyInfo[] properties = instanceType.GetProperties();

            foreach (PropertyInfo property in properties)
            {
                DotNetCobraType propertyType = DotNetCobraType.FromType(property.PropertyType);
                if(propertyType != null)
                    instance.DefineSymbol(property.Name, propertyType);
            }

            return instance;
        }
    }
}
