using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Emission;
using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Emission;

public class GetTypeByNameEmitterTests
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
    public void Emit_GetTypeByName_ReturnsSupportedTypeAndThrowsOnUnsupportedOrMissing()
    {
        var (typeBuilder, fields) = CreateTypeBuilderWithFields(("Name", typeof(string)), ("Description", typeof(string)), ("Age", typeof(int)));

        var method = new ModelGetTypeByNameMethod
        {
            Name = "GetStringByName",
            ReturnType = typeof(string)
        };

        var emitter = new GetTypeByNameEmitter(method, typeBuilder, fields, caseInsensitive: true);
        emitter.Emit();

        var type = typeBuilder.CreateType();
        var instance = Activator.CreateInstance(type)!;

        type.GetField("_name")!.SetValue(instance, "Alice");
        type.GetField("_description")!.SetValue(instance, "Manager");
        type.GetField("_age")!.SetValue(instance, 35);

        var getMethod = type.GetMethod("GetStringByName", [typeof(string)])!;
        Assert.NotNull(getMethod);

        Assert.Equal("Alice", getMethod.Invoke(instance, ["NAME"]));
        Assert.Equal("Manager", getMethod.Invoke(instance, ["Description"]));

        // Field exists but unsupported type -> InvalidOperationException
        var exIncompat = Assert.Throws<TargetInvocationException>(() => getMethod.Invoke(instance, ["Age"]));
        Assert.IsType<InvalidOperationException>(exIncompat.InnerException);

        // Field does not exist -> KeyNotFoundException
        var exNotFound = Assert.Throws<TargetInvocationException>(() => getMethod.Invoke(instance, ["Missing"]));
        Assert.IsType<KeyNotFoundException>(exNotFound.InnerException);
    }
}
