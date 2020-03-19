using System;

namespace CobraCompiler.Parse.TypeCheck.Types
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

        public Type Type { get; }
        public DotNetCobraType(string identifier, Type type) : base(identifier)
        {
            Type = type;
        }

        public override bool CanImplicitCast(CobraType other)
        {
            if (other is DotNetCobraType dotNetCobraType)
                return Type.IsAssignableFrom(dotNetCobraType.Type);

            return this == other;
        }

        public override CobraType GetSymbol(string symbol)
        {
            Type memberType = Type.GetProperty(symbol).PropertyType;

            return null;
        }

    }
}
