using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Parse.TypeCheck.Types
{
    class FuncGenericInstance: CobraGenericInstance
    {
        public FuncGenericInstance(string identifier, IEnumerable<CobraType> typeParams) : base(identifier, typeParams, DotNetCobraGeneric.FuncType)
        {
            AddCallSig(typeParams.ToList());
        }
    }
}
