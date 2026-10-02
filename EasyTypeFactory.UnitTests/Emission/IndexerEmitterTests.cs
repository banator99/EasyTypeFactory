using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Emission;
using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Emission;

public class IndexerEmitterTests
{
    public interface ITestIndexer
    {
        object this[string key] { get; set; }
    }

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
    public void Emit_IndexerWithGetAndSet_AllowsGettingAndSettingByIndexer()
    {
        var (typeBuilder, fields) = CreateTypeBuilderWithFields(("City", typeof(string), false));

        var indexer = new ModelIndexer { HasSet = true };
        var emitter = new IndexerEmitter(indexer, typeBuilder, fields, caseInsensitive: true);
        emitter.Emit();

        var type = typeBuilder.CreateType();
        var instance = Activator.CreateInstance(type)!;

        var indexerProperty = type.GetProperty("Item", [typeof(string)])!;
        Assert.NotNull(indexerProperty);
        Assert.NotNull(indexerProperty.GetGetMethod());
        Assert.NotNull(indexerProperty.GetSetMethod());

        indexerProperty.SetValue(instance, "Berlin", ["CITY"]);
        Assert.Equal("Berlin", indexerProperty.GetValue(instance, ["city"]));
    }

    [Fact]
    public void Emit_IndexerWithExplicitInterface_OverridesInterfaceMethods()
    {
        var (typeBuilder, fields) = CreateTypeBuilderWithFields(("Value", typeof(string), false));
        typeBuilder.AddInterfaceImplementation(typeof(ITestIndexer));

        var interfaceIndexer = typeof(ITestIndexer).GetProperty("Item", [typeof(string)])!;
        var indexer = new ModelIndexer
        {
            HasSet = true,
            OverriddenGetMethod = interfaceIndexer.GetGetMethod(),
            OverriddenSetMethod = interfaceIndexer.GetSetMethod()
        };

        var emitter = new IndexerEmitter(indexer, typeBuilder, fields, caseInsensitive: true);
        emitter.Emit();

        var type = typeBuilder.CreateType();
        var instance = (ITestIndexer)Activator.CreateInstance(type)!;

        instance["VALUE"] = "CustomValue";
        Assert.Equal("CustomValue", instance["value"]);
    }
}
