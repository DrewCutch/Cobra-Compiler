using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CobraCompiler.TypeCheck.Symbols;

namespace CobraCompiler.TypeCheck.Types
{
    class DotNetCobraType: CobraType, IDotNetType
    {
        public static DotNetCobraType Bool = new DotNetCobraType("bool", typeof(bool));
        public static DotNetCobraType Int = new DotNetCobraType("int", typeof(int));
        public static DotNetCobraType Float = new DotNetCobraType("float", typeof(float));
        public static DotNetCobraType Str = new DotNetCobraType("str", typeof(string));
        public static DotNetCobraType Null = new DotNetCobraType("null", null);
        public static DotNetCobraType Object = new DotNetCobraType("obj", typeof(object));
        public static DotNetCobraType Unit = new DotNetCobraType("unit", typeof(void));

        public static DotNetCobraType[] DotNetCobraTypes =
        {
            Bool, Int, Float, Str, Null, Object, Unit
        };

        static DotNetCobraType()
        {
            foreach (DotNetCobraType cobraType in DotNetCobraTypes)
                cobraType.DefineSymbols();
        }

        public Type Type { get; }
        public DotNetCobraType(string identifier, Type type) : base(identifier)
        {
            Type = type;
        }

        public override bool CanCastTo(CobraType other)
        {
            if (other is DotNetCobraType dotNetCobraType)
                return dotNetCobraType.Type.IsAssignableFrom(Type);

            return base.CanCastTo(other);
        }

        public override Symbol GetSymbol(string symbol)
        {
            //Type memberType = Type.GetProperty(symbol).PropertyType;

            return base.GetSymbol(symbol);
        }

        public static DotNetCobraType FromType(Type type)
        {
            foreach (DotNetCobraType dotNetCobraType in DotNetCobraTypes)
            {
                if (type == dotNetCobraType.Type)
                    return dotNetCobraType;
            }

            return null;
        }

        public void DefineSymbols()
        {
            if (Type == null)
                return;

            //TODO: assign proper mutability
            foreach (MemberInfo member in Type.GetMembers())
            {
                switch (member)
                {
                    case FieldInfo field:
                        if (FromType(field.FieldType) != null)
                            DefineSymbol(field.Name, new Symbol(null, FromType(field.FieldType), Mutability.CompileTimeConstant, field.Name));
                        break;
                    case MethodInfo method:
                        CobraType returnType = FromType(method.ReturnType);
                        List<CobraType> paramTypes = method.GetParameters().Select(parameter => FromType(parameter.ParameterType)).Cast<CobraType>().ToList();
                        if (!paramTypes.TrueForAll(element => element != null) || returnType == null)
                            continue;
                        paramTypes.Add(returnType);
                        DefineSymbol(method.Name, new Symbol(null, DotNetCobraGeneric.FuncType.CreateGenericInstance(paramTypes), Mutability.CompileTimeConstant, method.Name), true);
                        break;
                    case PropertyInfo property:
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
