using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.TypeCheck;
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

        private readonly Dictionary<CobraType, GenericTypeParameterBuilder> _interfaceGenerics;

        public InterfaceAssembler(string @namespace, CobraType cobraType, TypeStore typeStore, ModuleBuilder moduleBuilder)
        {
            _namespace = @namespace;
            _cobraType = cobraType;
            _typeStore = typeStore;
            _moduleBuilder = moduleBuilder;

            _interfaceGenerics = new Dictionary<CobraType, GenericTypeParameterBuilder>();
        }

        public TypeBuilder AssembleDefinition()
        {
            string identifier = _cobraType.IsConstructedGeneric
                ? _cobraType.GenericBase.Identifier
                : _cobraType.Identifier;

            _typeBuilder = _moduleBuilder.DefineType(_namespace + "." + identifier,
                TypeAttributes.Abstract | TypeAttributes.Interface | TypeAttributes.Public);


            if (_cobraType.IsGenericType)
            {
                GenericTypeParameterBuilder[] genericParams = _typeBuilder.DefineGenericParameters(_cobraType.TypeParams.Select(param => param.Identifier).ToArray());
                CobraType[] placeholders = new CobraType[genericParams.Length];

                for (int i = 0; i < _cobraType.TypeParams.Count; i++)
                {
                    placeholders[i] = CobraType.GenericPlaceholder(_cobraType.TypeParams[i].Identifier, i);
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

            foreach (CobraType parent in _cobraType.Parents)
                _typeBuilder.AddInterfaceImplementation(_typeStore.GetType(parent));

            foreach (KeyValuePair<string, Symbol> symbol in _cobraType.Symbols)
            {
                if (symbol.Value.Type.IsCallable())
                {
                    foreach (IReadOnlyList<CobraType> sig in symbol.Value.Type.CallSigs)
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

                    Type returnType = _typeStore.GetType(symbol.Value.Type);
                    PropertyBuilder propertyBuilder = _typeBuilder.DefineProperty(symbol.Key, PropertyAttributes.None, returnType, null);
                    MethodBuilder getMethod = PropertyAssembler.DefineGetMethod(_typeBuilder, symbol.Key, returnType, true);
                    propertyBuilder.SetGetMethod(getMethod);

                    _typeStore.AddTypeMember(_cobraType, symbol.Value.Type, propertyBuilder);
                }
            }

            _typeBuilder.CreateType();

            _typeStore.PopGenerics(_interfaceGenerics);
        }

        public override string ToString()
        {
            return $"InterfaceAssembler({_cobraType.Identifier})";
        }
    }
}
