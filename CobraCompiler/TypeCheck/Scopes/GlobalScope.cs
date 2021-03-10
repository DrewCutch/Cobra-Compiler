using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Statements;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.Parse.Scopes
{
    class GlobalScope: Scope
    {
        public GlobalScope() : base(null, (Statement)null)
        {
        }

        protected override bool IsTypeDefined(string identifier)
        {
            return _types.ContainsKey(identifier) || Type.GetType(identifier) != null;
        }

        protected override CobraType GetSimpleType(TypeInitExpression typeInit, CobraType selfHint = null)
        {
            if (typeInit.IdentifierStr == null)
                return null;

            string idStr = typeInit.IsNullable ? typeInit.IdentifierStr.Substring(0, typeInit.IdentifierStr.Length - 1) : typeInit.IdentifierStr;
            CobraType type = null;

            if (_types.ContainsKey(idStr))
                type = _types[idStr];

            if (type != null && typeInit.IsNullable)
                type = CobraType.Nullable(type);

            if (Type.GetType(idStr) is Type coreType)
            {
                DotNetCobraType cobraType = new DotNetCobraType(idStr, coreType);
                _types[idStr] = cobraType;
                return cobraType;
            }

            return type;
        }
    }
}
