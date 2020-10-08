using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Scanning;
using CobraCompiler.TypeCheck.Definers;
using CobraCompiler.TypeCheck.Exceptions;
using CobraCompiler.TypeCheck.Symbols;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.TypeCheck
{
    class TypeDefiner: IDefine
    {
        private readonly TypeDeclarationStatement _typeDeclaration;
        private readonly Scope _scope;
        private readonly bool _isGenericType;

        private readonly CobraType _type;

        public TypeDefiner(TypeDeclarationStatement typeDeclaration, Scope scope)
        {
            _typeDeclaration = typeDeclaration;

            _isGenericType = typeDeclaration.TypeArguments.Count > 0;

            List<GenericTypeParamPlaceholder> typeParams = new List<GenericTypeParamPlaceholder>();
            if (_isGenericType)
            {
                int i = 0;
                foreach (Token typeArgument in typeDeclaration.TypeArguments)
                {
                    typeParams.Add(new GenericTypeParamPlaceholder(typeArgument.Lexeme, i));
                    i++;
                }
            }

            if (_isGenericType)
                scope = TypeChecker.PushGenericScope(typeDeclaration, typeDeclaration.TypeArguments, scope);


            CobraType newType = _isGenericType ?
                new CobraGeneric(typeDeclaration.Name.Lexeme, typeParams) :
                new CobraType(typeDeclaration.Name.Lexeme);

            if (_isGenericType)
                scope.Parent.DefineType(typeDeclaration.Name.Lexeme, newType);
            else
                scope.DefineType(typeDeclaration.Name.Lexeme, newType);

            _type = newType;
            _scope = scope;
        }


        public void Define()
        {
            foreach (TypeInitExpression parent in _typeDeclaration.Parents)
            {
                if (!_scope.IsTypeDefined(parent))
                    throw new TypeNotDefinedException(parent);

                _type.AddParent(_scope.GetType(parent));
            }

            foreach (PropertyDefinitionExpression property in _typeDeclaration.Interface?.Properties ?? new List<PropertyDefinitionExpression>())
            {
                if (!_scope.IsTypeDefined(property.Type))
                    throw new TypeNotDefinedException(property.Type);

                CobraType propType = _scope.GetType(property.Type);

                bool isFunction = propType is FuncGenericInstance;
                string propName = property.Identifier.Lexeme;

                Mutability propMutability = isFunction
                    ? Mutability.CompileTimeConstant
                    : Mutability.Mutable;

                _type.DefineSymbol(propName, new Symbol(new ExpressionStatement(property), propType, propMutability, propName), isFunction);
            }
        }
    }
}
