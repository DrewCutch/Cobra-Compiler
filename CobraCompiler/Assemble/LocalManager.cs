using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.Assemble
{
    class LocalManager
    {
        private static readonly OpCode[] LoadArgShortCodes = {
            OpCodes.Ldarg_0, OpCodes.Ldarg_1, OpCodes.Ldarg_2, OpCodes.Ldarg_3
        };

        private readonly Dictionary<Scope, Dictionary<string, LocalBuilder>> _locals;
        private readonly TypeStore _typeStore;

        private readonly FuncScope _funcScope;
        private readonly ILGenerator _il;

        private readonly CobraType _classType;
        private bool IsClassMethod => _classType != null;

        public LocalManager(FuncScope funcScope, TypeStore typeStore, ILGenerator il, Type returnType)
        {
            _funcScope = funcScope;
            _locals = new Dictionary<Scope, Dictionary<string, LocalBuilder>>();
            _typeStore = typeStore;
            _il = il;

            ClassScope classScope = GetParentClassScope(funcScope);
            if(classScope != null)
                _classType = classScope.ThisType;

            if(returnType != typeof(void))
                DeclareVar(funcScope, "@ret", returnType);
        }

        private static ClassScope GetParentClassScope(Scope scope)
        {
            switch (scope.Parent)
            {
                case null:
                    return null;
                case ClassScope classScope:
                    return classScope;
                default:
                    return GetParentClassScope(scope.Parent);
            }
        }

        public void PrepStoreField(string name)
        {
            if(IsClassField(name))
                _il.Emit(OpCodes.Ldarg_0);
        }

        private bool IsClassField(string name) => IsClassMethod && _typeStore.TypeMemberExists(_classType, name, _funcScope.GetVar(name)?.Type);

        private FieldInfo GetClassField(string name)
        {
            if (!IsClassField(name))
                return null;

            FieldInfo fieldBuilder = (FieldInfo) _typeStore.GetMemberInfo(_classType, name, _funcScope.GetVar(name).Type);

            return fieldBuilder;
        }

        public void LoadVarAddress(Scope scope, string name)
        {
            LoadVarCore(scope, name, true);
        }

        public void LoadVar(Scope scope, string name)
        {
            LoadVarCore(scope, name, false);
        }

        private void LoadVarCore(Scope scope, string name, bool loadAddress)
        {
            int argPos = _funcScope.GetParamPosition(name);
            if (argPos != -1)
            {
                if(loadAddress)
                    _il.Emit(OpCodes.Ldarga, argPos);
                else if (argPos < LoadArgShortCodes.Length)
                    _il.Emit(LoadArgShortCodes[argPos]);
                else
                    _il.Emit(OpCodes.Ldarg, argPos);
                return;
            }

            if (LocalExists(scope, name))
                LoadLocal(scope, name, loadAddress);
            else if (IsClassField(name))
                LoadField(name, loadAddress);
        }

        private void LoadField(string name, bool loadAddress)
        {
            if(!IsClassField(name))
                throw new ArgumentOutOfRangeException(nameof(name));

            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(loadAddress ? OpCodes.Ldflda : OpCodes.Ldfld, GetClassField(name));
        }

        private void LoadLocal(Scope scope, string name, bool loadAddress)
        {
            LocalBuilder local = GetLocal(scope, name);
            
            if (local != null)
                _il.Emit(loadAddress ? OpCodes.Ldloca : OpCodes.Ldloc, local);
        }

        public void StoreVar(Scope scope, string name)
        {
            int argPos = _funcScope.GetParamPosition(name);
            
            if(argPos != -1)
                _il.Emit(OpCodes.Starg, argPos);

            if(LocalExists(scope, name))
                StoreLocal(scope, name);
            else if(IsClassField(name))
                StoreField(name);
        }

        private void StoreLocal(Scope scope, string name)
        {
            LocalBuilder local = GetLocal(scope, name);

            if (local != null) { }
                _il.Emit(OpCodes.Stloc, local);
        }

        private void StoreField(string name)
        {
            if(!IsClassField(name))
                throw new ArgumentOutOfRangeException(nameof(name));

            _il.Emit(OpCodes.Stfld, GetClassField(name));
        }

        public bool LocalExists(Scope scope, string name) => GetLocal(scope, name) != null;

        public LocalBuilder GetLocal(Scope scope, string name)
        {
            if(!_locals.ContainsKey(scope))
                _locals[scope] = new Dictionary<string, LocalBuilder>();

            if (_locals[scope].ContainsKey(name))
                return _locals[scope][name];

            if (scope.Parent != null)
                return GetLocal(scope.Parent, name);

            return null;
        }

        public void DeclareVar(Scope scope, string name, Type type)
        {
            if (!_locals.ContainsKey(scope))
                _locals[scope] = new Dictionary<string, LocalBuilder>();

            _locals[scope][name] = _il.DeclareLocal(type);
        }

        public void LoadLiteral(object value)
        {
            if (value == null)
            {
                _il.Emit(OpCodes.Ldnull);
                return;
            }

            switch (value)
            {
                case string str:
                    _il.Emit(OpCodes.Ldstr, str);
                    break;
                case float flt:
                    _il.Emit(OpCodes.Ldc_R4, flt);
                    break;
                case int intVal:
                    _il.Emit(OpCodes.Ldc_I4, intVal);
                    break;
                case bool boolVal:
                    _il.Emit(boolVal ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    break;
                case object unit:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
