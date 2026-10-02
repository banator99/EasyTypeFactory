using System;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory;
using Xunit;

namespace EasyTypeFactory.UnitTests;

public class CustomMethodBuilderTests
{
    [Fact]
    public void BuilderMethods_SetPropertiesCorrectly()
    {
        var builder = new CustomMethodBuilder("TestMethod");
        var updated = builder
            .WithAttributes(MethodAttributes.Private | MethodAttributes.Virtual)
            .WithReturnType(typeof(int))
            .WithParameterTypes(typeof(string), typeof(int))
            .ModifyMethod((mb) => { })
            .ModifyMethod((mb, ctx) => { })
            .WithEmitter((il) => il.Emit(OpCodes.Ret))
            .WithEmitter((il, ctx) => il.Emit(OpCodes.Ret));

        var concreteBuilder = (CustomMethodBuilder)updated;
        var def = concreteBuilder.ValidateAndGetDefinition();

        Assert.Equal("TestMethod", def.Name);
        Assert.Equal(MethodAttributes.Private | MethodAttributes.Virtual, def.Attributes);
        Assert.Equal(typeof(int), def.ReturnType);
        Assert.Equal([typeof(string), typeof(int)], def.ParameterTypes);
        Assert.Equal(2, def.Modifiers.Count);
        Assert.NotNull(def.Emitter);
    }

    [Fact]
    public void ValidateAndGetDefinition_WithoutEmitter_ThrowsArgumentNullException()
    {
        var builder = new CustomMethodBuilder("TestMethod");
        Assert.Throws<ArgumentNullException>(() => builder.ValidateAndGetDefinition());
    }
}
