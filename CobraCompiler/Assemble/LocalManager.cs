using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.TypeCheck;

namespace CobraCompiler.Assemble
{
    class LocalManager
    {
        private static readonly OpCode[] LoadArgShortCodes = {
            OpCodes.Ldarg_0, OpCodes.Ldarg_1, OpCodes.Ldarg_2, OpCodes.Ldarg_3
        };

        private readonly Dictionary<Scope, Dictionary<string, LocalBuilder>> _locals;
        private readonly FuncScope _funcScope;
        private readonly ILGenerator _il;

        public LocalManager(FuncScope funcScope, ILGenerator il)
        {
            _funcScope = funcScope;
            _locals = new Dictionary<Scope, Dictionary<string, LocalBuilder>>();
            _il = il;
        }

        public LocalManager(FuncScope funcScope, ILGenerator il, Type returnType)
        {
            _funcScope = funcScope;
            _locals = new Dictionary<Scope, Dictionary<string, LocalBuilder>>();
            _il = il;

            DeclareVar(funcScope, "@ret", returnType);
        }

        public void LoadVar(Scope scope, string name)
        {
            int argPos = _funcScope.GetParamPosition(name);
            if (argPos == -1)
            {
                LoadLocal(scope, name);
                return;
            }

            if (argPos < LoadArgShortCodes.Length)
                _il.Emit(LoadArgShortCodes[argPos]);
            else
                _il.Emit(OpCodes.Ldarg, argPos);
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

            _il.Emit(OpCodes.Starg, argPos);
        }

        private void StoreLocal(Scope scope, string name)
        {
            LocalBuilder local = GetLocal(scope, name);

            if (local != null)
                _il.Emit(OpCodes.Stloc, local);
        }

        public LocalBuilder GetLocal(Scope scope, string name)
        {
            if(!_locals.ContainsKey(scope))
                _locals[scope] = new Dictionary<string, LocalBuilder>();

            var localDict = _locals[scope];

            for (Scope localScope = scope; localScope != null && !localDict.ContainsKey(name); localScope = localScope.Parent)
            {
                localDict = _locals[localScope];
            }

            if (localDict.ContainsKey(name))
                return localDict[name];

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
            }
        }
    }
}
