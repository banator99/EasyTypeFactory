using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EmissionUtils = EasyTypeFactory.Emission.Utils;
using Xunit;

namespace EasyTypeFactory.UnitTests.Emission;

public class EmissionUtilsTests
{
    [Fact]
    public void Attributes_And_Methods_AreNotNull()
    {
        Assert.NotNull(EmissionUtils.RequiredMemberAttribute);
        Assert.NotNull(EmissionUtils.SetsRequiredMembersAttribute);
        Assert.NotNull(EmissionUtils.StringConcat3Method);
        Assert.NotNull(EmissionUtils.InvalidOperationExceptionCtor);
        Assert.NotNull(EmissionUtils.TypeIsAssignableFromMethod);
        Assert.NotNull(EmissionUtils.HashCodeGenericAddMethod);
        Assert.NotNull(EmissionUtils.HashCodeFinishMethod);
        Assert.NotNull(EmissionUtils.ObjectGetHashCodeMethod);
        Assert.NotNull(EmissionUtils.ObjectEqualsMethod);
    }

    [Fact]
    public void GetFullName_ReturnsExpectedString()
    {
        Assert.Equal(typeof(int).FullName, EmissionUtils.GetFullName(typeof(int)));
        Assert.Equal(typeof(List<string>).FullName, EmissionUtils.GetFullName(typeof(List<string>)));
    }

    [Fact]
    public void EmitThrowFieldNotFoundException_ThrowsKeyNotFoundException()
    {
        var assemblyName = new AssemblyName("TestAssembly_" + Guid.NewGuid().ToString("N"));
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        var typeBuilder = moduleBuilder.DefineType("TestType_" + Guid.NewGuid().ToString("N"), TypeAttributes.Public | TypeAttributes.Class);

        var method = typeBuilder.DefineMethod("ThrowNotFound", MethodAttributes.Public | MethodAttributes.Static, typeof(void), [typeof(string)]);
        var il = method.GetILGenerator();
        EmissionUtils.EmitThrowFieldNotFoundException(il, 0);

        var type = typeBuilder.CreateType();
        var methodInfo = type.GetMethod("ThrowNotFound")!;

        var ex = Assert.Throws<TargetInvocationException>(() => methodInfo.Invoke(null, ["MyMissingField"]));
        Assert.IsType<KeyNotFoundException>(ex.InnerException);
        Assert.Contains("MyMissingField", ex.InnerException.Message);
    }

    [Fact]
    public void EmitThrowFieldTypeNotCompatibleException_ThrowsInvalidOperationException()
    {
        var assemblyName = new AssemblyName("TestAssembly_" + Guid.NewGuid().ToString("N"));
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        var typeBuilder = moduleBuilder.DefineType("TestType_" + Guid.NewGuid().ToString("N"), TypeAttributes.Public | TypeAttributes.Class);

        var method1 = typeBuilder.DefineMethod("ThrowIncompat1", MethodAttributes.Public | MethodAttributes.Static, typeof(void), [typeof(string)]);
        var il1 = method1.GetILGenerator();
        EmissionUtils.EmitThrowFieldTypeNotCompatibleException(il1, 0, typeof(int));

        var method2 = typeBuilder.DefineMethod("ThrowIncompat2", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
        var il2 = method2.GetILGenerator();
        EmissionUtils.EmitThrowFieldTypeNotCompatibleException(il2, "FieldX", typeof(string), typeof(int));

        var type = typeBuilder.CreateType();

        var m1 = type.GetMethod("ThrowIncompat1")!;
        var ex1 = Assert.Throws<TargetInvocationException>(() => m1.Invoke(null, ["FieldName"]));
        Assert.IsType<InvalidOperationException>(ex1.InnerException);

        var m2 = type.GetMethod("ThrowIncompat2")!;
        var ex2 = Assert.Throws<TargetInvocationException>(() => m2.Invoke(null, null));
        Assert.IsType<InvalidOperationException>(ex2.InnerException);
    }
}
