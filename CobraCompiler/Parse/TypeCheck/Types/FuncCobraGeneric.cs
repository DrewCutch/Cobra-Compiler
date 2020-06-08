using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Parse.TypeCheck.Types
{
    class FuncCobraGeneric: DotNetCobraGeneric
    {
        public FuncCobraGeneric() : base("f", -1, Expression.GetDelegateType)
        {

        }

        public override CobraGenericInstance CreateGenericInstance(IReadOnlyList<CobraType> typeParams)
        {
            return new FuncGenericInstance(GenerateGenericInstanceName(typeParams), typeParams);
        }
    }
}
