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
                List<GenericTypeParamPlaceholder> typeParamPlaceholders = new List<GenericTypeParamPlaceholder>();
                for (int i = 0; i < classDeclaration.TypeArguments.Count; i++)
                {
                    typeParamPlaceholders.Add(new GenericTypeParamPlaceholder(classDeclaration.TypeArguments[i].Lexeme, i));
                }

                ThisType = new CobraGeneric("this", typeParamPlaceholders);
            }
            else
                ThisType = new CobraType("this", parentScope.GetType(classDeclaration.Type));

            // Call base method to avoid virtual member call in constructor (overridden version is not needed)
            base.Declare(classDeclaration, "this", ThisType, Mutability.AssignOnce, false);
        }

        protected internal override void Declare(Statement statement, string var, CobraType type, Mutability mutability, bool overload = false)
        {
            if(var == "init" && type is CobraGenericInstance genericInstance && genericInstance.Base == FuncCobraGeneric.FuncType)
                Parent.Declare(statement, ClassDeclaration.Name.Lexeme, type, mutability, overload);


            ThisType.DefineSymbol(var, new Symbol(statement, type, mutability, var), type is FuncGenericInstance);
            base.Declare(statement, var, type, mutability, overload);
        }
    }
}
