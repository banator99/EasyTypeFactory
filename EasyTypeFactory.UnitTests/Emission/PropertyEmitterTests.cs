using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using EasyTypeFactory.Emission;
using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Emission;

public class PropertyEmitterTests
{
    public interface ITestPropertyEntity
    {
        string Name { get; set; }
    }

    [Fact]
    public void Emit_ReadWriteProperty_EmitsGetterAndSetter()
    {
        var assemblyName = new AssemblyName("TestAssembly_" + Guid.NewGuid().ToString("N"));
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        var typeBuilder = moduleBuilder.DefineType("TestType_" + Guid.NewGuid().ToString("N"), TypeAttributes.Public | TypeAttributes.Class);

        var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
        var ctorIl = ctor.GetILGenerator();
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
        ctorIl.Emit(OpCodes.Ret);

        var field = new ModelField { Name = "_name", OriginalName = "Name", Type = typeof(string), ReadOnly = false, Required = true };
        var fieldBuilder = typeBuilder.DefineField("_name", typeof(string), FieldAttributes.Private);

        var prop = new ModelProperty
        {
            Name = "Name",
            Field = field,
            HasSet = true
        };

        var emitter = new PropertyEmitter(typeBuilder, prop, fieldBuilder);
        emitter.Emit();

        var type = typeBuilder.CreateType();
        var instance = Activator.CreateInstance(type)!;

        var propertyInfo = type.GetProperty("Name")!;
        Assert.NotNull(propertyInfo);
        Assert.NotNull(propertyInfo.GetGetMethod());
        Assert.NotNull(propertyInfo.GetSetMethod());
        Assert.NotNull(propertyInfo.GetCustomAttribute<RequiredMemberAttribute>());

        propertyInfo.SetValue(instance, "John");
        Assert.Equal("John", propertyInfo.GetValue(instance));
    }

    [Fact]
    public void Emit_ReadOnlyProperty_EmitsOnlyGetter()
    {
        var assemblyName = new AssemblyName("TestAssembly_" + Guid.NewGuid().ToString("N"));
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        var typeBuilder = moduleBuilder.DefineType("TestType_" + Guid.NewGuid().ToString("N"), TypeAttributes.Public | TypeAttributes.Class);

        var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
        var ctorIl = ctor.GetILGenerator();
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
        ctorIl.Emit(OpCodes.Ret);

        var field = new ModelField { Name = "_id", OriginalName = "Id", Type = typeof(int), ReadOnly = true };
        var fieldBuilder = typeBuilder.DefineField("_id", typeof(int), FieldAttributes.Public);

        var prop = new ModelProperty
        {
            Name = "Id",
            Field = field,
            HasSet = false
        };

        var emitter = new PropertyEmitter(typeBuilder, prop, fieldBuilder);
        emitter.Emit();

        var type = typeBuilder.CreateType();
        var instance = Activator.CreateInstance(type)!;

        var propertyInfo = type.GetProperty("Id")!;
        Assert.NotNull(propertyInfo);
        Assert.NotNull(propertyInfo.GetGetMethod());
        Assert.Null(propertyInfo.GetSetMethod());

        type.GetField("_id")!.SetValue(instance, 999);
        Assert.Equal(999, propertyInfo.GetValue(instance));
    }

    [Fact]
    public void Emit_ExplicitInterfaceImplementation_EmitsPrivateOverriddenMethods()
    {
        var assemblyName = new AssemblyName("TestAssembly_" + Guid.NewGuid().ToString("N"));
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        var typeBuilder = moduleBuilder.DefineType("TestType_" + Guid.NewGuid().ToString("N"), TypeAttributes.Public | TypeAttributes.Class);
        typeBuilder.AddInterfaceImplementation(typeof(ITestPropertyEntity));

        var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
        var ctorIl = ctor.GetILGenerator();
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
        ctorIl.Emit(OpCodes.Ret);

        var interfaceProp = typeof(ITestPropertyEntity).GetProperty("Name")!;
        var field = new ModelField { Name = "_name", OriginalName = "Name", Type = typeof(string), ReadOnly = false };
        var fieldBuilder = typeBuilder.DefineField("_name", typeof(string), FieldAttributes.Private);

        var prop = new ModelProperty
        {
            Name = "Name",
            Field = field,
            HasSet = true,
            ExplicitImplementationType = typeof(ITestPropertyEntity),
            OverriddenGetMethod = interfaceProp.GetGetMethod(),
            OverriddenSetMethod = interfaceProp.GetSetMethod()
        };

        var emitter = new PropertyEmitter(typeBuilder, prop, fieldBuilder);
        emitter.Emit();

        var type = typeBuilder.CreateType();
        var instance = Activator.CreateInstance(type)!;
        var entity = (ITestPropertyEntity)instance;

        entity.Name = "ExplicitVal";
        Assert.Equal("ExplicitVal", entity.Name);
    }
}
