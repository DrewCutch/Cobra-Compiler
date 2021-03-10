using System.Collections.Generic;
using System.Linq;

namespace CobraCompiler.TypeCheck.Types
{
    class FuncGenericInstance: CobraType
    {
        public FuncGenericInstance(string identifier, IReadOnlyList<CobraType> typeParams) : 
            base(identifier, new List<CobraType>(),  false, new List<CobraType>(), typeParams, DotNetCobraGeneric.FuncType, -1)
        {
            AddCallSig(typeParams.ToList());
        }
    }
}
