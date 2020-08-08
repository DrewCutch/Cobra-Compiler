using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.VisualStyles;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Scopes.ScopeReturn;
using CobraCompiler.Parse.Statements;
using CobraCompiler.TypeCheck.Operators;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.Parse.Scopes
{
    class Scope
    {
        protected readonly Dictionary<string, CobraType> _vars;
        protected readonly Dictionary<string, CobraType> _types;
        public HashSet<CobraType> DefinedTypes => new HashSet<CobraType>(_types.Values);

        protected readonly Dictionary<(Operation, CobraType, CobraType), IOperator> _operators;
        protected readonly Dictionary<(Operation, CobraType, CobraType), GenericOperator> _genericOperators;

        protected readonly List<Scope> _subScopes;

        public IReadOnlyList<Scope> SubScopes => _subScopes;
        public readonly Statement[] Body;

        public readonly Scope Parent;

        private ScopeReturnOrExpression _returns;
        public bool Returns => _returns.Returns;

        public Scope(Scope parentScope, Statement body)
        {
            Parent = parentScope;
            Body = new []{body};

            _vars = new Dictionary<string, CobraType>();
            _types = new Dictionary<string, CobraType>();

            _operators = new Dictionary<(Operation, CobraType, CobraType), IOperator>();
            _genericOperators = new Dictionary<(Operation, CobraType, CobraType), GenericOperator>();

            _subScopes = new List<Scope>();

            _returns = new ScopeReturnOrExpression(new ScopeReturnConstant(false));
        }

        public Scope(Scope parentScope, Statement[] body)
        {
            Parent = parentScope;
            Body = body;

            _vars = new Dictionary<string, CobraType>();
            _types = new Dictionary<string, CobraType>();

            _operators = new Dictionary<(Operation, CobraType, CobraType), IOperator>();
            _genericOperators = new Dictionary<(Operation, CobraType, CobraType), GenericOperator>();

            _subScopes = new List<Scope>();

            _returns = new ScopeReturnOrExpression(new ScopeReturnConstant(false));
        }

        public virtual void AddReturn()
        {
            _returns.AddOperand(new ScopeReturnConstant(true));
        }

        public void AddReturnExpression(ScopeReturnExpression expr)
        {
            _returns.AddOperand(expr);
        }

        public virtual CobraType GetType(TypeInitExpression typeInit, CobraType selfHint=null)
        {
            if (!typeInit.IsGenericInstance)
                return GetSimpleType(typeInit, selfHint);

            List<CobraType> paramTypes = typeInit.GenericParams.Select(param => GetType(param)).ToList();
            CobraGeneric generic = (CobraGeneric) GetSimpleType(typeInit.IdentifierStrWithoutParams);

            return generic.CreateGenericInstance(paramTypes);
        }

        protected virtual CobraType GetSimpleType(TypeInitExpression typeInit, CobraType selfHint = null)
        {
            if (typeInit.IdentifierStr == null)
                return null;

            if(_types.ContainsKey(typeInit.IdentifierStr))
                return _types[typeInit.IdentifierStr];

            return Parent?.GetSimpleType(typeInit);
        }

        protected virtual CobraType GetSimpleType(string typeIdentifier, CobraType selfHint = null)
        {
            if (typeIdentifier == null)
                return null;

            if (_types.ContainsKey(typeIdentifier))
                return _types[typeIdentifier];

            return Parent?.GetSimpleType(typeIdentifier, selfHint);
        }



        public virtual bool IsTypeDefined(TypeInitExpression typeInit)
        {
            if (!typeInit.IsGenericInstance)
                return IsTypeDefined(typeInit.IdentifierStr);

            bool allTypesDefined = IsTypeDefined(typeInit.IdentifierStrWithoutParams);

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

        public CobraType GetVarType(string identifier)
        {
            if (_vars.ContainsKey(identifier))
                return _vars[identifier];

            return Parent?.GetVarType(identifier);
        }

        public virtual void DefineType(string identifier, CobraType cobraType)
        {
            CobraTypeCobraType metaType = new CobraTypeCobraType(cobraType);
            _vars[identifier] = metaType;
            _types[identifier] = cobraType;
        }

        public void Declare(string var, TypeInitExpression typeInit, bool overload = false)
        {
            Declare(var, GetType(typeInit), overload);
        }

        public virtual void Declare(string var, CobraType type, bool overload = false)
        {
            if(IsDefined(var) && overload) 
                _vars[var] = IntersectionLangCobraGeneric.IntersectGeneric.CreateGenericInstance(GetVarType(var), type);
            else
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
            CobraType lhsBase = lhs;
            if (lhs is CobraGenericInstance lhsGeneric)
                lhsBase = lhsGeneric.Base;

            CobraType rhsBase = rhs;
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
            CobraType lhsBase = lhs;
            if (lhs is CobraGenericInstance lhsGeneric)
                lhsBase = lhsGeneric.Base;

            CobraType rhsBase = rhs;
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
