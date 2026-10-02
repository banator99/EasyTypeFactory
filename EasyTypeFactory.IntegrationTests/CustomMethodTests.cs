using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace EasyTypeFactory.IntegrationTests;

public class CustomMethodTests
{
    [Fact]
    public void AddCustomMethod_EmitsMethodAndExecutesIL()
    {
        var type = EasyTypeBuilder.Create()
            .WithParameterlessConstructor()
            .AddCustomMethod("MultiplyByThree", method => method
                .WithAttributes(MethodAttributes.Public | MethodAttributes.HideBySig)
                .WithReturnType(typeof(int))
                .WithParameterTypes(typeof(int))
                .WithEmitter((il, _) =>
                {
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldc_I4_3);
                    il.Emit(OpCodes.Mul);
                    il.Emit(OpCodes.Ret);
                }))
            .Build();

        var instance = Activator.CreateInstance(type)!;
        var multiplyMethod = type.GetMethod("MultiplyByThree", [typeof(int)])!;
        Assert.NotNull(multiplyMethod);

        var result = (int)multiplyMethod.Invoke(instance, [7])!;
        Assert.Equal(21, result);
    }

    [Fact]
    public void AddCustomMethod_WithMethodModifier_AppliesModifications()
    {
        var modified = false;

        var type = EasyTypeBuilder.Create()
            .AddCustomMethod("Compute", method => method
                .WithReturnType(typeof(string))
                .ModifyMethod(mb =>
                {
                    modified = true;
                    Assert.NotNull(mb);
                })
                .WithEmitter(il =>
                {
                    il.Emit(OpCodes.Ldstr, "Result");
                    il.Emit(OpCodes.Ret);
                }))
            .Build();

        Assert.True(modified);

        var instance = Activator.CreateInstance(type)!;
        var computeMethod = type.GetMethod("Compute", Type.EmptyTypes)!;
        Assert.Equal("Result", computeMethod.Invoke(instance, null));
    }

    [Fact]
    public void AddCustomMethod_WithoutEmitter_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            EasyTypeBuilder.Create()
                .AddCustomMethod("InvalidMethod", method => method
                    .WithReturnType(typeof(void)));
        });
    }

    [Fact]
    public void AddCustomMethod_RemoveAndClearCustomMethods_WorksCorrectly()
    {
        var builder = EasyTypeBuilder.Create()
            .AddCustomMethod("Method1", m => m.WithEmitter(il => il.Emit(OpCodes.Ret)))
            .AddCustomMethod("Method2", m => m.WithEmitter(il => il.Emit(OpCodes.Ret)))
            .RemoveCustomMethods("Method1");

        var type = builder.Build();
        Assert.Null(type.GetMethod("Method1"));
        Assert.NotNull(type.GetMethod("Method2"));

        var typeCleared = builder.ClearCustomMethods().Build();
        Assert.Null(typeCleared.GetMethod("Method2"));
    }
}
