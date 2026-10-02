using System;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory;
using EasyTypeFactory.Utils;
using Xunit;

namespace EasyTypeFactory.UnitTests;

public class MethodBuilderContextTests
{
    [Fact]
    public void Constructor_InitializesMethodBuilderProperty()
    {
        var typeBuilder = Shared.ModuleBuilder.DefineType("ContextTestType_" + Guid.NewGuid().ToString("N"));
        var methodBuilder = typeBuilder.DefineMethod("Test", MethodAttributes.Public, typeof(void), Type.EmptyTypes);

        var context = new MethodBuilderContext(methodBuilder);
        Assert.Same(methodBuilder, context.MethodBuilder);
    }
}
