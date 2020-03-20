using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Assemble.LangTypeAssemblers
{
    class UnionAssembler
    {
        private const TypeAttributes UnionTypeAttributes =
            TypeAttributes.Public | TypeAttributes.SpecialName;

        private const MethodAttributes NestedTypeFactoryAttributes =
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Static;

        private const TypeAttributes TagsTypeAttributes =
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SpecialName | TypeAttributes.NestedPublic;

        private const FieldAttributes TagAttributes =
            FieldAttributes.Public | FieldAttributes.Literal;

        private const TypeAttributes NestedTypeAttributes =
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SpecialName | TypeAttributes.NestedPublic;

        private const FieldAttributes ItemAttributes =
            FieldAttributes.Assembly | FieldAttributes.InitOnly;

        private const MethodAttributes ItemCtorAttributes =
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

        private const MethodAttributes ItemGetAttributes =
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

        public static Type Assemble(ModuleBuilder mb, params Type[] typeParams)
        {
            string name = typeParams.Aggregate("@Union", (current, typeParam) => current + $"_{typeParam.Name}");
            TypeBuilder union = mb.DefineType(name, UnionTypeAttributes);
            ConstructorBuilder ctor = union.DefineDefaultConstructor(ItemCtorAttributes);

            GenerateTags(union, typeParams);
            List<TypeBuilder> nestedTypes = GenerateNestedTypes(union, ctor, typeParams);

            union.CreateType();

            foreach (TypeBuilder nestedType in nestedTypes)
            {
                nestedType.CreateType();
            }
            return union;
            //throw new NotImplementedException();
        }

        private static void GenerateTags(TypeBuilder typeBuilder, params Type[] typeParams)
        {
            TypeBuilder tags = typeBuilder.DefineNestedType("Tags", TagsTypeAttributes);
            
            for(int i = 0; i < typeParams.Length; i++)
            {
                FieldBuilder tag = tags.DefineField(typeParams[i].Name, typeof(int), TagAttributes);
                tag.SetConstant(i);
            }

            tags.CreateType();
        }

        private static List<TypeBuilder> GenerateNestedTypes(TypeBuilder typeBuilder, ConstructorInfo baseCtor, params Type[] typeParams)
        {
            List<TypeBuilder> nested = new List<TypeBuilder>();

            foreach (Type typeParam in typeParams)
            {
                TypeBuilder nestedType = typeBuilder.DefineNestedType(typeParam.Name, NestedTypeAttributes, typeBuilder);

                FieldBuilder item = nestedType.DefineField("item", typeParam, ItemAttributes);

                ConstructorBuilder ctor = nestedType.DefineConstructor(ItemCtorAttributes, CallingConventions.Standard,
                    new Type[] {typeParam});
                // ctor.DefineParameter(0, )

                ILGenerator ctorIL = ctor.GetILGenerator();

                ctorIL.Emit(OpCodes.Ldarg_0);
                ctorIL.Emit(OpCodes.Call, baseCtor);
                ctorIL.Emit(OpCodes.Ldarg_0);
                ctorIL.Emit(OpCodes.Ldarg_1);
                ctorIL.Emit(OpCodes.Stfld, item);
                ctorIL.Emit(OpCodes.Ret);


                MethodBuilder getItem = nestedType.DefineMethod("GetItem", ItemGetAttributes, typeParam, new Type[]{});
                ILGenerator getItemIL = getItem.GetILGenerator();

                getItemIL.Emit(OpCodes.Ldarg_0); // this
                getItemIL.Emit(OpCodes.Ldfld, item); // this.item
                getItemIL.Emit(OpCodes.Ret); // return


                MethodBuilder nestedFactory = typeBuilder.DefineMethod($"New{typeParam.Name}", NestedTypeFactoryAttributes, nestedType, new Type[] { typeParam});
                ILGenerator factoryIL = nestedFactory.GetILGenerator();
                factoryIL.DeclareLocal(nestedType);
                factoryIL.Emit(OpCodes.Ldarg_0);
                factoryIL.Emit(OpCodes.Newobj, ctor);
                factoryIL.Emit(OpCodes.Ret);

                nested.Add(nestedType);
            }

            return nested;
        }
    }
}
