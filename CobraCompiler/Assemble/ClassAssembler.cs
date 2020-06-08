using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Parse.TypeCheck.Types;

namespace CobraCompiler.Assemble
{
    class ClassAssembler: IAssemble
    {
        private const TypeAttributes ClassAttributes = TypeAttributes.Class | TypeAttributes.Public;
        private const FieldAttributes MemberAttributes = FieldAttributes.Private;
        private const MethodAttributes MethodsAttributes = MethodAttributes.Virtual | MethodAttributes.Final  | MethodAttributes.HideBySig;
        private const MethodAttributes GetterAttributes = MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Public;

        private const MethodAttributes InitAttributes =
            MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly ModuleBuilder _moduleBuilder;
        private readonly ClassScope _classScope;
        private readonly TypeStore _typeStore;
        private readonly MethodStore _methodStore;

        private TypeBuilder _typeBuilder;
        private List<IAssemble> _members;

        private enum MethodType
        {
            Private,
            Public
        }

        public ClassAssembler(ClassScope classScope, TypeStore typeStore, MethodStore methodStore, AssemblyBuilder assemblyBuilder, ModuleBuilder moduleBuilder)
        {
            _classScope = classScope;
            _typeStore = typeStore;
            _methodStore = methodStore;
            _assemblyBuilder = assemblyBuilder;
            _moduleBuilder = moduleBuilder;
        }

        public TypeBuilder AssembleDefinition()
        {
            ClassDeclarationStatement declaration = _classScope.ClassDeclaration;
            Type[] interfaces = { _typeStore.GetType(_classScope.GetType(declaration.Type)) };
            _typeBuilder =
                _moduleBuilder.DefineType(declaration.Name.Lexeme, ClassAttributes, typeof(object), interfaces);

            AssembleDefinitions();

            return _typeBuilder;
        }

        public void AssembleDefinitions()
        {
            _members = new List<IAssemble>();

            foreach (Scope scope in _classScope.SubScopes)
            {
                switch (scope)
                {
                    case FuncScope methodScope:
                        _members.Add(AssembleMethodDefinition(methodScope));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(scope));
                }
            }

            // If the body is not a block statement that means the class consists of a single method
            if (_classScope.Body is BlockStatement classBlock)
                foreach (Statement statement in classBlock.Body)
                    AssembleStatement(statement);
        }

        private FuncAssembler AssembleMethodDefinition(FuncScope funcScope)
        {
            FuncAssembler methodAssembler = CreateMethodAssembler(funcScope, MethodType.Public);
            MethodBase methodBase = methodAssembler.AssembleDefinition();

            if(funcScope.FuncDeclaration is InitDeclarationStatement)
                _methodStore.AddMethodInfo(_classScope.ClassDeclaration.Name.Lexeme, funcScope.FuncType, methodBase);
            
            return methodAssembler;
        }

        private FuncAssembler CreateMethodAssembler(FuncScope funcScope, MethodType methodType)
        {
            MethodAttributes attributes = funcScope.FuncDeclaration is InitDeclarationStatement
                ? InitAttributes
                : MethodsAttributes;

            return new FuncAssembler(funcScope, _typeStore, _methodStore, _typeBuilder, _assemblyBuilder,
                attributes
                | (methodType == MethodType.Public ? MethodAttributes.Public : MethodAttributes.Private));
        }

        public void Assemble()
        {
            foreach (IAssemble member in _members)
                member.Assemble();

            _typeBuilder.CreateType();
        }

        private void AssembleStatement(Statement statement)
        {
            switch (statement)
            {
                case VarDeclarationStatement varDeclarationStatement:
                    AssembleVarDeclaration(varDeclarationStatement);
                    break;
                case InitDeclarationStatement _:
                case FuncDeclarationStatement _:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statement));
            }
        }

        private void AssembleVarDeclaration(VarDeclarationStatement declaration)
        {
            CobraType cobraType = _classScope.GetType(declaration.TypeInit);

            Type varType = _typeStore.GetType(cobraType);

            FieldBuilder field = _typeBuilder.DefineField(declaration.Name.Lexeme, varType, MemberAttributes);

            _typeStore.AddTypeMember(_classScope.ThisType, cobraType, field);

            if (IsTypeProperty(declaration))
            {
                MethodBuilder getMethod = PropertyAssembler.DefineGetMethod(_typeBuilder, declaration.Name.Lexeme, varType);
                ILGenerator il = getMethod.GetILGenerator();

                // return this.<field>
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Ret);

                PropertyBuilder property = _typeBuilder.DefineProperty(declaration.Name.Lexeme, PropertyAttributes.None, varType, Type.EmptyTypes);
                property.SetGetMethod(getMethod);
            }
        }

        private bool IsTypeProperty(VarDeclarationStatement declaration)
        {
            CobraType externalType = _classScope.GetType(_classScope.ClassDeclaration.Type);
            return externalType.HasSymbol(declaration.Name.Lexeme);
        }
    }
}
