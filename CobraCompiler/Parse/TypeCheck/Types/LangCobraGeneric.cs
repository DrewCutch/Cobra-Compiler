using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Assemble.LangTypeAssemblers;
using CobraCompiler.Util;

namespace CobraCompiler.Parse.TypeCheck.Types
{
    public delegate Type GenericTypeAssembler(ModuleBuilder mb, params Type[] typeParams);

    class LangCobraGeneric: CobraGeneric, ITypeGenerator
    {
        private readonly Dictionary<List<Type>, Type> _typeCache;

        private readonly GenericTypeAssembler _typeAssembler;

        public LangCobraGeneric(string identifier, int numberOfParams, GenericTypeAssembler instanceGenerator) : base(identifier, numberOfParams)
        {
            _typeAssembler = instanceGenerator;
            _typeCache = new Dictionary<List<Type>, Type>(new ListByElementComparer<Type>());
        }

        public Type GetType(ModuleBuilder mb, params Type[] typeArgs)
        {
            List<Type> typeList = new List<Type>(typeArgs);

            if (_typeCache.ContainsKey(typeList))
                return _typeCache[typeList];

            Type type = _typeAssembler(mb, typeArgs);

            _typeCache[typeList] = type;

            return type;
        }

    }
}
