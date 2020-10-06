using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.VisualStyles;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Statements;
using CobraCompiler.TypeCheck;
using CobraCompiler.TypeCheck.Operators;
using CobraCompiler.TypeCheck.Symbols;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.Parse.Scopes
{
    class Scope
    {
        protected readonly Dictionary<string, Symbol> _vars;
        protected readonly Dictionary<Symbol, List<Expression>> _references;
        protected readonly Dictionary<Symbol, List<AssignExpression>> _assignments;

        protected readonly Dictionary<string, CobraType> _types;
        public HashSet<CobraType> DefinedTypes => new HashSet<CobraType>(_types.Values);

        protected readonly Dictionary<(Operation, CobraType, CobraType), IOperator> _operators;
        protected readonly Dictionary<(Operation, CobraType, CobraType), GenericOperator> _genericOperators;

        protected readonly List<Scope> _subScopes;

        public IReadOnlyList<Scope> SubScopes => _subScopes;
        public readonly Statement[] Body;

        public readonly Scope Parent;

        public Scope(Scope parentScope, Statement body)
        {
            Parent = parentScope;
            Body = new []{body};

            _vars = new Dictionary<string, Symbol>();
            _types = new Dictionary<string, CobraType>();

            _operators = new Dictionary<(Operation, CobraType, CobraType), IOperator>();
            _genericOperators = new Dictionary<(Operation, CobraType, CobraType), GenericOperator>();

            _subScopes = new List<Scope>();
        }

        public Scope(Scope parentScope, Statement[] body)
        {
            Parent = parentScope;
            Body = body;

            _vars = new Dictionary<string, Symbol>();
            _types = new Dictionary<string, CobraType>();

            _operators = new Dictionary<(Operation, CobraType, CobraType), IOperator>();
            _genericOperators = new Dictionary<(Operation, CobraType, CobraType), GenericOperator>();

            _subScopes = new List<Scope>();
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

        public Symbol GetVar(string identifier)
        {
            if (_vars.ContainsKey(identifier))
                return _vars[identifier];

            return Parent?.GetVar(identifier);
        }

        public virtual void DefineType(string identifier, CobraType cobraType)
        {
            CobraTypeCobraType metaType = new CobraTypeCobraType(cobraType);
            _vars[identifier] = new Symbol(null, metaType, Mutability.CompileTimeConstant, identifier); //TODO: add definition
            _types[identifier] = cobraType;
        }

        public void Declare(ImportStatement importStatement, CobraType importType)
        {
            Declare(importStatement, ((GetExpression)importStatement.Import).Name.Lexeme, importType, Mutability.CompileTimeConstant);
        }
        public void Declare(ParamDeclarationStatement paramDeclaration)
        {
            Declare(paramDeclaration, paramDeclaration.Name.Lexeme, GetType(paramDeclaration.TypeInit), Mutability.AssignOnce);
        }

        public void Declare(VarDeclarationStatement varDeclaration)
        {
            Declare(varDeclaration, varDeclaration.Name.Lexeme, GetType(varDeclaration.TypeInit), Mutability.Mutable);
        }

        public void Declare(FuncDeclarationStatement funcDeclaration, CobraType funcType)
        {
            Declare(funcDeclaration, funcDeclaration.Name.Lexeme, funcType, Mutability.AssignOnce, true);
        }

        protected internal virtual void Declare(Statement expr, string var, CobraType type, Mutability mutability, bool overload = false)
        {
            if(IsDefined(var) && overload) 
                _vars[var] = new Symbol(expr, IntersectionLangCobraGeneric.IntersectGeneric.CreateGenericInstance(GetVar(var).Type, type), mutability, var);
            else
                _vars[var] = new Symbol(expr, type, mutability, var);
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
