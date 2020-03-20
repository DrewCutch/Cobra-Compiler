using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Parse.TypeCheck.Types
{
    /*
     * Used for Generics generating Types not yet representable by Cobra Typing Features
     */
    interface ITypeGenerator
    {
        Type GetType(ModuleBuilder mb, params Type[] typeArgs);
    }
}
