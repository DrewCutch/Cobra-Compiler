using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Assemble
{
    class PropertyAssembler
    {
        private static readonly MethodAttributes PropertyGetAttributes =
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual;

        public static MethodBuilder DefineGetMethod(TypeBuilder typeBuilder, string symbolName, Type returnType, bool isAbstract=false)
        {
            return isAbstract
                ? DefineGetMethod(typeBuilder, symbolName, returnType, PropertyGetAttributes | MethodAttributes.Abstract | MethodAttributes.NewSlot)
                : DefineGetMethod(typeBuilder, symbolName, returnType, PropertyGetAttributes);
        }

        private static MethodBuilder DefineGetMethod(TypeBuilder typeBuilder, string symbolName, Type returnType, MethodAttributes attributes)
        {
            return typeBuilder.DefineMethod($"get_{symbolName}", attributes, returnType, Type.EmptyTypes);
        }
    }
}
