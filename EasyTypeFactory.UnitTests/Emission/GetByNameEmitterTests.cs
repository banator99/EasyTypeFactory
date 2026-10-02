using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Emission;
using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Emission;

public class GetByNameEmitterTests
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
    public void Emit_GetByName_ReturnsValuesAndThrowsOnMissing()
    {
        var (typeBuilder, fields) = CreateTypeBuilderWithFields(("Id", typeof(int)), ("Name", typeof(string)));

        var method = new ModelGetByNameMethod { Name = "GetByName" };
        var emitter = new GetByNameEmitter(method, typeBuilder, fields, caseInsensitive: true);
        emitter.Emit();

        var type = typeBuilder.CreateType();
        var instance = Activator.CreateInstance(type)!;

        type.GetField("_id")!.SetValue(instance, 42);
        type.GetField("_name")!.SetValue(instance, "Bob");

        var getByNameMethod = type.GetMethod("GetByName", [typeof(string)])!;
        Assert.NotNull(getByNameMethod);

        // Case-insensitive test
        Assert.Equal(42, getByNameMethod.Invoke(instance, ["id"]));
        Assert.Equal("Bob", getByNameMethod.Invoke(instance, ["NAME"]));

        // Missing field throws KeyNotFoundException
        var ex = Assert.Throws<TargetInvocationException>(() => getByNameMethod.Invoke(instance, ["NonExistent"]));
        Assert.IsType<KeyNotFoundException>(ex.InnerException);
    }
}
