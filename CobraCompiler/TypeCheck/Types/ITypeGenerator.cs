using System;
using System.Reflection.Emit;

namespace CobraCompiler.TypeCheck.Types
{
    /*
     * Used for Generics generating Types not yet representable by Cobra Typing Features
     */
    interface ITypeGenerator
    {
        Type GetType(ModuleBuilder mb, params Type[] typeArgs);
    }
}
