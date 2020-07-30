using System;
using System.Collections.Generic;
using System.Linq;

namespace CobraCompiler.TypeCheck.Types
{
    class CobraGenericInstance: CobraType
    {
        public readonly IReadOnlyList<CobraType> TypeParams;
        public readonly CobraGeneric Base;

        public CobraGenericInstance(string identifier, IEnumerable<CobraType> typeParams, CobraGeneric @base) : base(identifier)
        {
            TypeParams = new List<CobraType>(typeParams);
            Base = @base;
        }

        public CobraGenericInstance ReplacePlaceholders(List<CobraType> typeArguments)
        {
            List<CobraType> typeParams = new List<CobraType>();

            foreach (CobraType typeParam in TypeParams)
            {
                if(typeParam is GenericTypeParamPlaceholder placeholder)
                    typeParams.Add(typeArguments[placeholder.Index]);
                else if(typeParam is CobraGenericInstance genericInstance)
                    typeParams.Add(genericInstance.ReplacePlaceholders(typeArguments));
                else
                    typeParams.Add(typeParam);
            }

            return Base.CreateGenericInstance(typeParams);
        }

        public bool HasPlaceholders()
        {
            foreach (CobraType typeParam in TypeParams)
            {
                if (typeParam is GenericTypeParamPlaceholder placeholder)
                    return true;
            }

            return false;
        }

        public override bool Equals(Object other)
        {
            CobraGenericInstance otherInstance = other as CobraGenericInstance;

            if (otherInstance == null)
                return false;


            if (Identifier != otherInstance.Identifier)
                return false;

            return TypeParams.SequenceEqual(otherInstance.TypeParams);
        }

        public override int GetHashCode()
        {
            int hashCode = Identifier.GetHashCode();

            foreach (CobraType cobraType in TypeParams)
            {
                hashCode = hashCode * 31 + (cobraType == null ? 0 : cobraType.GetHashCode());
            }

            return hashCode;
        }
    }
}
