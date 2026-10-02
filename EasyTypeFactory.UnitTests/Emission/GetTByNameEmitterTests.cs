using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Emission;
using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Emission;

public class GetTByNameEmitterTests
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
    public void Emit_GetTByName_HandlesExactTypeAssignableTypeAndExceptions()
    {
        var (typeBuilder, fields) = CreateTypeBuilderWithFields(("Id", typeof(int)), ("Text", typeof(string)));

        var method = new ModelGetTByNameMethod { Name = "GetByName" };
        var emitter = new GetTByNameEmitter(method, typeBuilder, fields, caseInsensitive: false);
        emitter.Emit();

        var type = typeBuilder.CreateType();
        var instance = Activator.CreateInstance(type)!;

        type.GetField("_id")!.SetValue(instance, 100);
        type.GetField("_text")!.SetValue(instance, "Sample");

        var getByNameMethod = type.GetMethod("GetByName", [typeof(string)])!;
        Assert.NotNull(getByNameMethod);

        // Exact match
        var getIntMethod = getByNameMethod.MakeGenericMethod(typeof(int));
        Assert.Equal(100, getIntMethod.Invoke(instance, ["Id"]));

        // Assignable match (string -> object)
        var getObjMethod = getByNameMethod.MakeGenericMethod(typeof(object));
        Assert.Equal("Sample", getObjMethod.Invoke(instance, ["Text"]));

        // Incompatible type -> InvalidOperationException
        var getStringMethod = getByNameMethod.MakeGenericMethod(typeof(string));
        var exIncompat = Assert.Throws<TargetInvocationException>(() => getStringMethod.Invoke(instance, ["Id"]));
        Assert.IsType<InvalidOperationException>(exIncompat.InnerException);

        // Missing field -> KeyNotFoundException
        var exNotFound = Assert.Throws<TargetInvocationException>(() => getIntMethod.Invoke(instance, ["Unknown"]));
        Assert.IsType<KeyNotFoundException>(exNotFound.InnerException);
    }
}
