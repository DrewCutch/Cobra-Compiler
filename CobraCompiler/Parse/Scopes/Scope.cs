using System.Collections.Generic;
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
        protected readonly Dictionary<string, CobraGeneric> _generics;
        protected readonly Dictionary<(TokenType, CobraType, CobraType), Operator> _operators;
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
            _operators = new Dictionary<(TokenType, CobraType, CobraType), Operator>();

            _subScopes = new List<Scope>();
        }

        public virtual CobraType GetType(TypeInitExpression typeInit)
        {
            if (!typeInit.IsGenericInstance)
                return GetType(typeInit.IdentifierStr);

            List<CobraType> paramTypes = typeInit.GenericParams.Select(GetType).ToList();
            CobraGeneric generic = GetGeneric(typeInit.IdentifierStrWithoutParams);

            return generic.CreateGenericInstance(paramTypes);
        }

        protected virtual CobraType GetType(string identifier)
        {
            if (identifier == null)
                return null;

            if(_types.ContainsKey(identifier))
                return _types[identifier];

            return Parent?.GetType(identifier);
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

        public void DefineType(string identifier, CobraType cobraType)
        {
            _types[identifier] = cobraType;
        }

        public void Declare(string var, TypeInitExpression typeInit)
        {
            Declare(var, GetType(typeInit));
        }

        public void Declare(string var, CobraType type)
        {
            _vars[var] = type;
        }

        public bool IsDefined(string var)
        {
            if (_vars.ContainsKey(var))
                return true;

            return Parent != null && Parent.IsDeclared(var);
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

        public bool IsOperatorDefined(TokenType op, CobraType lhs, CobraType rhs)
        {
            if (_operators.ContainsKey((op, lhs, rhs)))
                return true;

            return Parent != null && Parent.IsOperatorDefined(op, lhs, rhs);
        }

        public Operator GetOperator(TokenType op, CobraType lhs, CobraType rhs)
        {
            if (_operators.ContainsKey((op, lhs, rhs)))
                return _operators[(op, lhs, rhs)];

            return Parent?.GetOperator(op, lhs, rhs);
        }

        public void DefineOperator(TokenType opToken, CobraType lhs, CobraType rhs, Operator op)
        {
            _operators[(opToken, lhs, rhs)] = op;
        }

        public virtual CobraType GetReturnType()
        {
            return Parent?.GetReturnType();
        }
    }
}
