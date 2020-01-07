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

        public static CobraGeneric[] BuiltInCobraGenerics =
        {
            FuncType
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

        public override CobraGenericInstance CreateGenericInstance(IReadOnlyList<CobraType> typeParams)
        {
            if (HasFixedParamCount && typeParams.Count > NumberOfParams)
                throw new ArgumentException("Invalid number of parameters");

            return new CobraGenericInstance($"{Identifier}[{string.Join(",", typeParams.Select(param => param.Identifier))}]", typeParams, this);
        }
    }
}
