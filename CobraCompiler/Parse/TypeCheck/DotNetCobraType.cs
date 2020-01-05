using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.TypeCheck;

namespace CobraCompiler.Parse.TypeCheck
{
    class DotNetCobraType: CobraType
    {
        public static DotNetCobraType Bool = new DotNetCobraType("bool", typeof(bool));
        public static DotNetCobraType Int = new DotNetCobraType("int", typeof(int));
        public static DotNetCobraType Float = new DotNetCobraType("float", typeof(float));
        public static DotNetCobraType Str = new DotNetCobraType("str", typeof(string));
        public static DotNetCobraType Null = new DotNetCobraType("null", null);

        public static DotNetCobraType[] DotNetCobraTypes =
        {
            Bool, Int, Float, Str, Null
        };

        public readonly Type Type;
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
    }
}
