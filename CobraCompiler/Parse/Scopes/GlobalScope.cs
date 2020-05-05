using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Parse.TypeCheck;
using CobraCompiler.Parse.TypeCheck.Types;

namespace CobraCompiler.Parse.Scopes
{
    class GlobalScope: Scope
    {
        public GlobalScope() : base(null, null)
        {
        }

        protected override bool IsTypeDefined(string identifier)
        {
            return _types.ContainsKey(identifier) || Type.GetType(identifier) != null;
        }

        protected override CobraType GetSimpleType(TypeInitExpression typeInit, string self="")
        {
            if (typeInit.IdentifierStr == null)
                return null;

            if (_types.ContainsKey(typeInit.IdentifierStr))
                return _types[typeInit.IdentifierStr];

            if (Type.GetType(typeInit.IdentifierStr) is Type coreType)
            {
                DotNetCobraType cobraType = new DotNetCobraType(typeInit.IdentifierStr, coreType);
                _types[typeInit.IdentifierStr] = cobraType;
                return cobraType;
            }

            return null;
        }
    }
}
