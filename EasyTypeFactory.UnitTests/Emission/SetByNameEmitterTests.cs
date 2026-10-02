using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Emission;
using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Emission;

public class SetByNameEmitterTests
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
    public void Emit_SetByName_SetsValuesAndHandlesNullAndExceptions()
    {
        var (typeBuilder, fields) = CreateTypeBuilderWithFields(
            ("Age", typeof(int), false),
            ("Name", typeof(string), false),
            ("ReadOnlyId", typeof(int), true));

        var method = new ModelSetByNameMethod { Name = "SetByName" };
        var emitter = new SetByNameEmitter(method, typeBuilder, fields, caseInsensitive: true);
        emitter.Emit();

        var type = typeBuilder.CreateType();
        var instance = Activator.CreateInstance(type)!;

        var setMethod = type.GetMethod("SetByName", [typeof(string), typeof(object)])!;
        Assert.NotNull(setMethod);

        // Set value type (boxed int)
        setMethod.Invoke(instance, ["age", 25]);
        Assert.Equal(25, type.GetField("_age")!.GetValue(instance));

        // Set reference type
        setMethod.Invoke(instance, ["NAME", "Charlie"]);
        Assert.Equal("Charlie", type.GetField("_name")!.GetValue(instance));

        // Set null on reference type
        setMethod.Invoke(instance, ["Name", null]);
        Assert.Null(type.GetField("_name")!.GetValue(instance));

        // Incompatible type -> InvalidOperationException
        var exIncompat = Assert.Throws<TargetInvocationException>(() => setMethod.Invoke(instance, ["Age", "NotAnInt"]));
        Assert.IsType<InvalidOperationException>(exIncompat.InnerException);

        // ReadOnly field -> InvalidOperationException
        var exReadOnly = Assert.Throws<TargetInvocationException>(() => setMethod.Invoke(instance, ["ReadOnlyId", 50]));
        Assert.IsType<InvalidOperationException>(exReadOnly.InnerException);

        // Missing field -> KeyNotFoundException
        var exNotFound = Assert.Throws<TargetInvocationException>(() => setMethod.Invoke(instance, ["Unknown", 1]));
        Assert.IsType<KeyNotFoundException>(exNotFound.InnerException);
    }
}
