﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Parse.TypeCheck;

namespace CobraCompiler.Parse.Scopes
{
    class GlobalScope: Scope
    {
        public GlobalScope() : base(null, null)
        {
        }

        public override bool IsTypeDefined(string identifier)
        {
            return _types.ContainsKey(identifier) || Type.GetType(identifier) != null;
        }

        public override CobraType GetType(string identifier)
        {
            if (identifier == null)
                return null;

            if (_types.ContainsKey(identifier))
                return _types[identifier];

            if (Type.GetType(identifier) is Type coreType)
            {
                DotNetCobraType cobraType = new DotNetCobraType(identifier, coreType);
                _types[identifier] = cobraType;
                return cobraType;
            }

            return null;
        }
    }
}
