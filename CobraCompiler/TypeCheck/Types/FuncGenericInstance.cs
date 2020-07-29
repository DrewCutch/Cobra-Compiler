﻿using System.Collections.Generic;
using System.Linq;

namespace CobraCompiler.TypeCheck.Types
{
    class FuncGenericInstance: CobraGenericInstance
    {
        public FuncGenericInstance(string identifier, IEnumerable<CobraType> typeParams) : base(identifier, typeParams, DotNetCobraGeneric.FuncType)
        {
            AddCallSig(typeParams.ToList());
        }
    }
}