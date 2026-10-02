using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Emission;
using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Emission;

public class SetTypeByNameEmitterTests
{
    private static (TypeBuilder TypeBuilder, Dictionary<ModelField, FieldBuilder> Fields) CreateTypeBuilderWithFields(
        params (string Name, Type Type, bool ReadOnly)[] fieldDefs)
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
        foreach (var (name, type, readOnly) in fieldDefs)
        {
            var modelField = new ModelField
            {
                Name = "_" + name.ToLowerInvariant(),
                OriginalName = name,
                Type = type,
                ReadOnly = readOnly
            };
            var fieldBuilder = typeBuilder.DefineField(
                modelField.Name,
                type,
                FieldAttributes.Public | (readOnly ? FieldAttributes.InitOnly : 0));
            fields.Add(modelField, fieldBuilder);
        }

        return (typeBuilder, fields);
    }

    [Fact]
    public void Emit_SetTypeByName_SetsMatchingTypeAndHandlesErrors()
    {
        var (typeBuilder, fields) = CreateTypeBuilderWithFields(
            ("Name", typeof(string), false),
            ("ReadOnlyName", typeof(string), true),
            ("Age", typeof(int), false));

        var method = new ModelSetTypeByNameMethod
        {
            Name = "SetStringByName",
            Parameters = [typeof(string), typeof(string)]
        };

        var emitter = new SetTypeByNameEmitter(method, typeBuilder, fields, caseInsensitive: true);
        emitter.Emit();

        var type = typeBuilder.CreateType();
        var instance = Activator.CreateInstance(type)!;

        var setMethod = type.GetMethod("SetStringByName", [typeof(string), typeof(string)])!;
        Assert.NotNull(setMethod);

        // Set matching field
        setMethod.Invoke(instance, ["NAME", "UpdatedName"]);
        Assert.Equal("UpdatedName", type.GetField("_name")!.GetValue(instance));

        // ReadOnly field -> InvalidOperationException
        var exReadOnly = Assert.Throws<TargetInvocationException>(() => setMethod.Invoke(instance, ["ReadOnlyName", "Val"]));
        Assert.IsType<InvalidOperationException>(exReadOnly.InnerException);

        // Incompatible field -> InvalidOperationException
        var exIncompat = Assert.Throws<TargetInvocationException>(() => setMethod.Invoke(instance, ["Age", "Val"]));
        Assert.IsType<InvalidOperationException>(exIncompat.InnerException);

        // Missing field -> KeyNotFoundException
        var exNotFound = Assert.Throws<TargetInvocationException>(() => setMethod.Invoke(instance, ["Unknown", "Val"]));
        Assert.IsType<KeyNotFoundException>(exNotFound.InnerException);
    }
}
