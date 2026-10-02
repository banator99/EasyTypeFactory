using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Emission;
using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Emission;

public class SetTByNameEmitterTests
{
    private static (TypeBuilder TypeBuilder, Dictionary<ModelField, FieldBuilder> Fields) CreateTypeBuilderWithFields(
        params (string Name, Type Type)[] fieldDefs)
    {
        var assemblyName = new AssemblyName("TestAssembly_" + Guid.NewGuid().ToString("N"));
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        var typeBuilder = moduleBuilder.DefineType("TestType_" + Guid.NewGuid().ToString("N"), TypeAttributes.Public | TypeAttributes.Class);

        var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
        var il = ctor.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
        il.Emit(OpCodes.Ret);

        var fields = new Dictionary<ModelField, FieldBuilder>();
        foreach (var (name, type) in fieldDefs)
        {
            var modelField = new ModelField
            {
                Name = "_" + name.ToLowerInvariant(),
                OriginalName = name,
                Type = type
            };
            var fieldBuilder = typeBuilder.DefineField(modelField.Name, type, FieldAttributes.Public);
            fields.Add(modelField, fieldBuilder);
        }

        return (typeBuilder, fields);
    }

    [Fact]
    public void Emit_SetTByName_SetsExactAndDerivedTypesAndThrowsOnIncompatible()
    {
        var (typeBuilder, fields) = CreateTypeBuilderWithFields(("Score", typeof(int)), ("Obj", typeof(object)));

        var method = new ModelSetTByNameMethod { Name = "SetByName" };
        var emitter = new SetTByNameEmitter(method, typeBuilder, fields, caseInsensitive: false);
        emitter.Emit();

        var type = typeBuilder.CreateType();
        var instance = Activator.CreateInstance(type)!;

        var setMethod = type.GetMethod("SetByName", [typeof(string), Type.MakeGenericMethodParameter(0)])!;
        Assert.NotNull(setMethod);

        // Exact match for int
        var setInt = setMethod.MakeGenericMethod(typeof(int));
        setInt.Invoke(instance, ["Score", 88]);
        Assert.Equal(88, type.GetField("_score")!.GetValue(instance));

        // Derived/assignable match for object
        var setObjString = setMethod.MakeGenericMethod(typeof(string));
        setObjString.Invoke(instance, ["Obj", "HelloObj"]);
        Assert.Equal("HelloObj", type.GetField("_obj")!.GetValue(instance));

        // Incompatible type -> InvalidOperationException
        var setIncompat = setMethod.MakeGenericMethod(typeof(string));
        var exIncompat = Assert.Throws<TargetInvocationException>(() => setIncompat.Invoke(instance, ["Score", "Invalid"]));
        Assert.IsType<InvalidOperationException>(exIncompat.InnerException);

        // Missing field -> KeyNotFoundException
        var exNotFound = Assert.Throws<TargetInvocationException>(() => setInt.Invoke(instance, ["NonExistent", 10]));
        Assert.IsType<KeyNotFoundException>(exNotFound.InnerException);
    }
}
