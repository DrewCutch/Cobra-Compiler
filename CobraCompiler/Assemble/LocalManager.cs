﻿using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Scopes;

namespace CobraCompiler.Assemble
{
    class LocalManager
    {
        private static readonly OpCode[] LoadArgShortCodes = {
            OpCodes.Ldarg_0, OpCodes.Ldarg_1, OpCodes.Ldarg_2, OpCodes.Ldarg_3
        };

        private readonly Dictionary<Scope, Dictionary<string, LocalBuilder>> _locals;
        private readonly Dictionary<string, FieldBuilder> _fields;

        private readonly FuncScope _funcScope;
        private readonly ILGenerator _il;

        private readonly ClassScope _classScope;

        public LocalManager(FuncScope funcScope, ILGenerator il, Type returnType)
        {
            _funcScope = funcScope;
            _locals = new Dictionary<Scope, Dictionary<string, LocalBuilder>>();
            _fields = new Dictionary<string, FieldBuilder>();
            _il = il;
            // TODO: Implement FieldStore
            if(returnType != typeof(void))
                DeclareVar(funcScope, "@ret", returnType);
        }

        private bool IsClassField(string name) => _fields.ContainsKey(name);

        public void LoadVar(Scope scope, string name)
        {
            int argPos = _funcScope.GetParamPosition(name);
            if (argPos != -1)
            {
                if (argPos < LoadArgShortCodes.Length)
                    _il.Emit(LoadArgShortCodes[argPos]);
                else
                    _il.Emit(OpCodes.Ldarg, argPos);
                return;
            }

            if (IsClassField(name))
                LoadField(name);
            else
                LoadLocal(scope, name);
        }

        private void LoadField(string name)
        {
            if(!IsClassField(name))
                throw new ArgumentOutOfRangeException(nameof(name));

            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, _fields[name]);
        }

        private void LoadLocal(Scope scope, string name)
        {
            LocalBuilder local = GetLocal(scope, name);
            

            if (local != null)
                _il.Emit(OpCodes.Ldloc, local);
        }

        public void StoreVar(Scope scope, string name)
        {
            int argPos = _funcScope.GetParamPosition(name);
            if (argPos == -1)
            {
                StoreLocal(scope, name);
                return;
            }

            if (IsClassField(name))
                StoreField(name);
            else
                _il.Emit(OpCodes.Starg, argPos);
        }

        private void StoreLocal(Scope scope, string name)
        {
            LocalBuilder local = GetLocal(scope, name);

            if (local != null)
                _il.Emit(OpCodes.Stloc, local);
        }

        private void StoreField(string name)
        {
            if(!IsClassField(name))
                throw new ArgumentOutOfRangeException(nameof(name));

            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Stfld, _fields[name]);
        }

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
                return;

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
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
