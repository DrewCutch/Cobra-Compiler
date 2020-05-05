using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Assemble.LangTypeAssemblers
{
    class IntersectionAssembler
    {
        private const TypeAttributes IntersectionTypeAttributes =
            TypeAttributes.Public | TypeAttributes.SpecialName | TypeAttributes.Abstract | TypeAttributes.Interface;
        public static Type Assemble(ModuleBuilder mb, params Type[] typeParams)
        {
            string name = typeParams.Aggregate("@Intersection", (current, typeParam) => current + $"_{typeParam.Name}");
            TypeBuilder intersection = mb.DefineType(name, IntersectionTypeAttributes);
            foreach (Type type in typeParams)
                intersection.AddInterfaceImplementation(type);

            intersection.CreateType();
            return intersection;
        }
    }
}
