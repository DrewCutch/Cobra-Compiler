using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Parse.TypeCheck.Types;

namespace CobraCompiler.Parse.Scopes
{
    class ClassScope: Scope
    {
        public readonly ClassDeclarationStatement ClassDeclaration;
        public readonly CobraType ThisType;

        public ClassScope(Scope parentScope, ClassDeclarationStatement classDeclaration) : base(parentScope, classDeclaration.Body)
        {
            ClassDeclaration = classDeclaration;
            ThisType = new CobraType("this", parentScope.GetType(classDeclaration.Type));
            Declare("this", ThisType);
        }

        public override void Declare(string var, CobraType type)
        {
            if(var == "init" && type is CobraGenericInstance genericInstance && genericInstance.Base == DotNetCobraGeneric.FuncType)
                Parent.Declare(ClassDeclaration.Name.Lexeme, type);


            ThisType.DefineSymbol(var, type);
            base.Declare(var, type);
        }
    }
}
