using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Emission;
using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Emission;

public class GetHashCodeEmitterTests
{
    private static (TypeBuilder TypeBuilder, Dictionary<ModelField, FieldBuilder> Fields) CreateTypeBuilderWithFields(
        params (string Name, Type Type)[] fieldDefs)
    {
        var assemblyName = new AssemblyName("TestAssembly_" + Guid.NewGuid().ToString("N"));
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        var typeBuilder = moduleBuilder.DefineType("TestType_" + Guid.NewGuid().ToString("N"), TypeAttributes.Public | TypeAttributes.Class);

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

        // Add a parameterless constructor so we can instantiate the object
        var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
        var il = ctor.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
        il.Emit(OpCodes.Ret);

        return (typeBuilder, fields);
    }

    [Fact]
    public void Emit_GetHashCode_ComputesHashCodeBasedOnFields()
    {
        var (typeBuilder, fields) = CreateTypeBuilderWithFields(("Id", typeof(int)), ("Name", typeof(string)));

        var emitter = new GetHashCodeEmitter(typeBuilder, fields);
        emitter.Emit();

        var generatedType = typeBuilder.CreateType();
        var instance1 = Activator.CreateInstance(generatedType)!;
        var instance2 = Activator.CreateInstance(generatedType)!;

        var idField = generatedType.GetField("_id")!;
        var nameField = generatedType.GetField("_name")!;

        idField.SetValue(instance1, 42);
        nameField.SetValue(instance1, "Test");

        idField.SetValue(instance2, 42);
        nameField.SetValue(instance2, "Test");

        // Physically call GetHashCode on generated objects
        var hash1 = instance1.GetHashCode();
        var hash2 = instance2.GetHashCode();

        Assert.Equal(hash1, hash2);

        idField.SetValue(instance2, 99);
        var hash3 = instance2.GetHashCode();
        Assert.NotEqual(hash1, hash3);
    }
}
