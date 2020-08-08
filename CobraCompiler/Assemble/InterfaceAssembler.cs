using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.Assemble
{
    class InterfaceAssembler: IAssemble
    {
        private readonly string _namespace;
        private readonly CobraType _cobraType;
        private readonly TypeStore _typeStore;
        private readonly ModuleBuilder _moduleBuilder;

        private TypeBuilder _typeBuilder;

        private readonly Dictionary<GenericTypeParamPlaceholder, GenericTypeParameterBuilder> _interfaceGenerics;

        public InterfaceAssembler(string @namespace, CobraType cobraType, TypeStore typeStore, ModuleBuilder moduleBuilder)
        {
            _namespace = @namespace;
            _cobraType = cobraType;
            _typeStore = typeStore;
            _moduleBuilder = moduleBuilder;

            _interfaceGenerics = new Dictionary<GenericTypeParamPlaceholder, GenericTypeParameterBuilder>();
        }

        public TypeBuilder AssembleDefinition()
        {
            string identifier = _cobraType is CobraGenericInstance genInst
                ? genInst.Base.Identifier
                : _cobraType.Identifier;

            _typeBuilder = _moduleBuilder.DefineType(_namespace + "." + identifier,
                TypeAttributes.Abstract | TypeAttributes.Interface | TypeAttributes.Public);


            if (_cobraType is CobraGeneric generic)
            {
                GenericTypeParameterBuilder[] genericParams = _typeBuilder.DefineGenericParameters(generic.TypeParams.Select(param => param.Identifier).ToArray());
                GenericTypeParamPlaceholder[] placeholders = new GenericTypeParamPlaceholder[genericParams.Length];

                for (int i = 0; i < generic.TypeParams.Count; i++)
                {
                    placeholders[i] = new GenericTypeParamPlaceholder(generic.TypeParams[i].Identifier, i);
                    _interfaceGenerics[placeholders[i]] = genericParams[i];
                }

                _typeStore.PushCurrentGenerics(_interfaceGenerics);
            }

            _typeStore.AddType(_cobraType, _typeBuilder);

            _typeStore.PopGenerics(_interfaceGenerics);

            return _typeBuilder;
        }

        public void Assemble()
        {
            _typeStore.PushCurrentGenerics(_interfaceGenerics);

            foreach (KeyValuePair<string, CobraType> symbol in _cobraType.Symbols)
            {
                if (symbol.Value.IsCallable())
                {
                    foreach (IReadOnlyList<CobraType> sig in symbol.Value.CallSigs)
                    {
                        Type returnType = _typeStore.GetType(sig.Last());
                        Type[] paramTypes = sig.Take(sig.Count - 1).Select(param => _typeStore.GetType(param)).ToArray();

                        MethodBuilder member = _typeBuilder.DefineMethod(symbol.Key,
                            MethodAttributes.Virtual | MethodAttributes.Abstract | MethodAttributes.Public | MethodAttributes.HideBySig |
                            MethodAttributes.NewSlot, CallingConventions.HasThis, returnType, paramTypes);

                        _typeStore.AddTypeMember(_cobraType, DotNetCobraGeneric.FuncType.CreateGenericInstance(sig), member);
                    }
                }
                else
                {

                    Type returnType = _typeStore.GetType(symbol.Value);
                    PropertyBuilder propertyBuilder = _typeBuilder.DefineProperty(symbol.Key, PropertyAttributes.None, returnType, null);
                    MethodBuilder getMethod = PropertyAssembler.DefineGetMethod(_typeBuilder, symbol.Key, returnType, true);
                    propertyBuilder.SetGetMethod(getMethod);

                    _typeStore.AddTypeMember(_cobraType, symbol.Value, propertyBuilder);
                }
            }

            _typeBuilder.CreateType();

            _typeStore.PopGenerics(_interfaceGenerics);
        }
    }
}
