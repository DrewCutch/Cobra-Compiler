using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Statements;
using CobraCompiler.TypeCheck;
using CobraCompiler.TypeCheck.Symbols;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.Parse.Scopes
{
    class ClassScope: Scope
    {
        public readonly ClassDeclarationStatement ClassDeclaration;
        public readonly CobraType ThisType;

        public ClassScope(Scope parentScope, ClassDeclarationStatement classDeclaration) : base(parentScope, classDeclaration.Body.Body.ToArray())
        {
            ClassDeclaration = classDeclaration;
            if (classDeclaration.TypeArguments.Count > 0)
            {
                List<CobraType> typeParamPlaceholders = new List<CobraType>();
                for (int i = 0; i < classDeclaration.TypeArguments.Count; i++)
                {
                    typeParamPlaceholders.Add(CobraType.GenericPlaceholder(classDeclaration.TypeArguments[i].Lexeme, i));
                }

                ThisType = CobraType.GenericCobraType(classDeclaration.Name.Lexeme + ".this", typeParamPlaceholders);
            }
            else
                ThisType = CobraType.BasicCobraType(classDeclaration.Name.Lexeme + ".this", parentScope.GetType(classDeclaration.Type));

            // Call base method to avoid virtual member call in constructor (overridden version is not needed)
            base.Declare(classDeclaration, "this", ThisType, SymbolKind.This, Mutability.AssignOnce, false);
        }

        protected internal override Symbol Declare(Statement statement, string var, CobraType type, SymbolKind kind, Mutability mutability, bool overload = false, Symbol aliasOf = null)
        {
            if(var == "init" && type.IsConstructedGeneric && type.GenericBase == FuncCobraGeneric.FuncType)
                Parent.Declare(statement, ClassDeclaration.Name.Lexeme, type, SymbolKind.Global, mutability, overload, aliasOf);

            Symbol scopeSymbol = base.Declare(statement, var, type, SymbolKind.ThisMember, mutability, overload, aliasOf);

            ThisType.DefineSymbol(var, scopeSymbol, type is FuncGenericInstance);

            return scopeSymbol;
        }
    }
}
