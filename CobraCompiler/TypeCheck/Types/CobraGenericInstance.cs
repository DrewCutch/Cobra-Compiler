﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Forms;

namespace CobraCompiler.TypeCheck.Types
{
    class CobraGenericInstance: CobraType
    {
        public readonly IReadOnlyList<CobraType> OrderedTypeParams;
        public readonly Dictionary<GenericTypeParamPlaceholder, CobraType> TypeParams;
        public readonly CobraGeneric Base;

        public CobraGenericInstance(string identifier, IReadOnlyList<CobraType> typeParams, CobraGeneric @base) : base(identifier)
        {
            OrderedTypeParams = new List<CobraType>(typeParams);
            TypeParams = @base.CreateTypeParamMap(typeParams);
            Base = @base;
        }

        public CobraGenericInstance ReplacePlaceholders(List<CobraType> typeArguments)
        {
            List<CobraType> typeParams = new List<CobraType>();

            foreach (CobraType typeParam in OrderedTypeParams)
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

        public bool HasPlaceholders() => OrderedTypeParams.Any(param => param is GenericTypeParamPlaceholder);

        public override CobraType GetSymbol(string symbol)
        {
            CobraType baseSymbol = Base.GetSymbol(symbol);

            if (baseSymbol is GenericTypeParamPlaceholder typeParam)
                return TypeParams[typeParam];

            if (baseSymbol is CobraGeneric genericSymbol)
                return genericSymbol.CreateGenericInstance(TypeParams);

            if (baseSymbol == null)
                return base.GetSymbol(symbol);

            return baseSymbol;
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

            foreach (CobraType cobraType in OrderedTypeParams)
            {
                hashCode = hashCode * 31 + (cobraType == null ? 0 : cobraType.GetHashCode());
            }

            return hashCode;
        }
    }
}
