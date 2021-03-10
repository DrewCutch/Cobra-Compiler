using System.Collections.Generic;
using System.Linq.Expressions;

namespace CobraCompiler.TypeCheck.Types
{
    class FuncCobraGeneric: DotNetCobraGeneric
    {
        public FuncCobraGeneric() : base("f", -1, Expression.GetDelegateType)
        {

        }

        public override CobraType CreateGenericInstance(IReadOnlyList<CobraType> typeParams)
        {
            return new FuncGenericInstance(GenerateGenericInstanceName(typeParams), typeParams);
        }
    }
}
