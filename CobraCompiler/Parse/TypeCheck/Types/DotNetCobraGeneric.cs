using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CobraCompiler.Parse.TypeCheck.Types
{
    public delegate Type GenericInstanceGenerator(params Type[] typeArgs);

    class DotNetCobraGeneric: CobraGeneric
    {
        public static DotNetCobraGeneric FuncType = new DotNetCobraGeneric("f", -1, Expression.GetDelegateType);
        public static DotNetCobraGeneric ListType = new DotNetCobraGeneric("list", 1, typeof(List<>).MakeGenericType);
        public static CobraGeneric[] BuiltInCobraGenerics =
        {
            FuncType,
            ListType
        };

        public readonly GenericInstanceGenerator InstanceGenerator;

        public DotNetCobraGeneric(string identifier, int numberOfParams, GenericInstanceGenerator instanceGenerator) : base(identifier, numberOfParams)
        {
            InstanceGenerator = instanceGenerator;
        }

        public Type GetType(params Type[] typeArgs)
        {
            return InstanceGenerator(typeArgs);
        }
    }
}
