﻿using System.Collections.Generic;
using System.Linq;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Parse.TypeCheck;
using CobraCompiler.Parse.TypeCheck.Operators;
using CobraCompiler.Parse.TypeCheck.Types;

namespace CobraCompiler.Parse.Scopes
{
    class Scope
    {
        protected readonly Dictionary<string, CobraType> _vars;
        protected readonly Dictionary<string, CobraType> _types;
        public HashSet<CobraType> DefinedTypes => new HashSet<CobraType>(_types.Values);

        protected readonly Dictionary<string, CobraGeneric> _generics;

        protected readonly Dictionary<(Operation, CobraType, CobraType), IOperator> _operators;
        protected readonly Dictionary<(Operation, CobraTypeBase, CobraTypeBase), GenericOperator> _genericOperators;

        protected readonly List<Scope> _subScopes;

        public IReadOnlyList<Scope> SubScopes => _subScopes;
        public readonly Statement Body;

        public readonly Scope Parent;

        public Scope(Scope parentScope, Statement body)
        {
            Parent = parentScope;
            Body = body;

            _vars = new Dictionary<string, CobraType>();
            _types = new Dictionary<string, CobraType>();

            _generics = new Dictionary<string, CobraGeneric>();
            _operators = new Dictionary<(Operation, CobraType, CobraType), IOperator>();
            _genericOperators = new Dictionary<(Operation, CobraTypeBase, CobraTypeBase), GenericOperator>();

            _subScopes = new List<Scope>();
        }

        public virtual CobraType GetType(TypeInitExpression typeInit)
        {
            if (!typeInit.IsGenericInstance)
                return GetSimpleType(typeInit);

            List<CobraType> paramTypes = typeInit.GenericParams.Select(GetType).ToList();
            CobraGeneric generic = GetGeneric(typeInit.IdentifierStrWithoutParams);

            return generic.CreateGenericInstance(paramTypes);
        }

        private CobraType CreateInterface(InterfaceDefinitionExpression interfaceDefinitionExpression)
        {
            CobraType interfaceType = new CobraType(interfaceDefinitionExpression.IdentifierStr);

            foreach (PropertyDefinitionExpression property in interfaceDefinitionExpression.Properties)
            {
                interfaceType.DefineSymbol(property.Identifier.Lexeme, GetType(property.Type));
            }

            _types[interfaceDefinitionExpression.IdentifierStr] = interfaceType;

            return interfaceType;
        }

        protected virtual CobraType GetSimpleType(TypeInitExpression typeInit)
        {
            if (typeInit.IdentifierStr == null)
                return null;

            if(_types.ContainsKey(typeInit.IdentifierStr))
                return _types[typeInit.IdentifierStr];

            if (typeInit is InterfaceDefinitionExpression interfaceDefinition)
                return CreateInterface(interfaceDefinition);

            return Parent?.GetSimpleType(typeInit);
        }

        public virtual bool IsTypeDefined(TypeInitExpression typeInit)
        {
            if (!typeInit.IsGenericInstance)
                return IsTypeDefined(typeInit.IdentifierStr);

            bool allTypesDefined = IsGenericDefined(typeInit.IdentifierStrWithoutParams);

            foreach (TypeInitExpression typeParam in typeInit.GenericParams)
            {
                allTypesDefined = allTypesDefined && IsTypeDefined(typeParam);
            }

            return allTypesDefined;
        }

        protected virtual bool IsTypeDefined(string identifier)
        {
            return _types.ContainsKey(identifier) || (Parent != null && Parent.IsTypeDefined(identifier));
        }

        public CobraGeneric GetGeneric(string identifier)
        {
            if (_generics.ContainsKey(identifier))
                return _generics[identifier];

            return Parent?.GetGeneric(identifier);
        }

        public void DefineGeneric(string identifier, CobraGeneric generic)
        {
            _generics[identifier] = generic;
        }

        protected virtual bool IsGenericDefined(string identifier)
        {
            return _generics.ContainsKey(identifier) ||  (Parent?.IsGenericDefined(identifier) ?? false);
        }

        public CobraType GetVarType(string identifier)
        {
            if (_vars.ContainsKey(identifier))
                return _vars[identifier];

            return Parent?.GetVarType(identifier);
        }

        public virtual void DefineType(string identifier, CobraType cobraType)
        {
            _types[identifier] = cobraType;
        }

        public void Declare(string var, TypeInitExpression typeInit)
        {
            Declare(var, GetType(typeInit));
        }

        public virtual void Declare(string var, CobraType type)
        {
            _vars[var] = type;
        }

        public bool IsDefined(string var)
        {
            if (_vars.ContainsKey(var))
                return true;

            return Parent != null && Parent.IsDefined(var);
        }

        public bool IsDeclared(string var)
        {
            return _vars.ContainsKey(var);
        }

        public CobraGenericInstance GetGenericInstance(string identifier, IReadOnlyList<CobraType> typeParameters)
        {
            return GetGeneric(identifier).CreateGenericInstance(typeParameters);
        }

        public void AddSubScope(Scope scope)
        {
            _subScopes.Add(scope);
        }

        public bool IsOperatorDefined(Operation op, CobraType lhs, CobraType rhs)
        {
            return GetOperator(op, lhs, rhs) != null;
        }

        public IOperator GetOperator(Operation op, CobraType lhs, CobraType rhs)
        {
            CobraTypeBase lhsBase = lhs;
            if (lhs is CobraGenericInstance lhsGeneric)
                lhsBase = lhsGeneric.Base;

            CobraTypeBase rhsBase = rhs;
            if (rhs is CobraGenericInstance rhsGeneric)
                rhsBase = rhsGeneric;

            if (_genericOperators.ContainsKey((op, lhsBase, rhsBase)))
                return _genericOperators[(op, lhsBase, rhsBase)].GetOperatorInstance(lhs, rhs);

            if (_operators.ContainsKey((op, lhs, rhs)))
                return _operators[(op, lhs, rhs)];

            return Parent?.GetOperator(op, lhs, rhs);
        }

        public BinaryOperator? GetGenericBinaryOperator(Operation op, CobraType lhs, CobraType rhs)
        {
            CobraTypeBase lhsBase = lhs;
            if (lhs is CobraGenericInstance lhsGeneric)
                lhsBase = lhsGeneric.Base;

            CobraTypeBase rhsBase = rhs;
            if (rhs is CobraGenericInstance rhsGeneric)
                rhsBase = rhsGeneric;

            if (_genericOperators.ContainsKey((op, lhsBase, rhsBase)))
                return _genericOperators[(op, lhsBase, rhsBase)].GetGenericBinaryOperator();

            return Parent?.GetGenericBinaryOperator(op, lhs, rhs);
        }

        public void DefineOperator(GenericOperator op)
        {
            _genericOperators[(op.Operation, op.Lhs, op.Rhs)] = op;
        }

        public void DefineOperator(BinaryOperator op)
        {
            DefineOperator(op.Operation, op.Lhs, op.Rhs, op);
        }

        public void DefineOperator(Operation operation, CobraType lhs, CobraType rhs, IOperator op)
        {
            _operators[(operation, lhs, rhs)] = op;
        }

        public virtual CobraType GetReturnType()
        {
            return Parent?.GetReturnType();
        }
    }
}
