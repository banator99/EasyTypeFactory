using System;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Model;

public class ModelCustomMethodTests
{
    [Fact]
    public void Properties_AreCorrectlyInitialized()
    {
        var method = new ModelCustomMethod
        {
            Name = "Custom",
            ReturnType = typeof(string),
            Parameters = [typeof(int)],
            Attributes = MethodAttributes.Public | MethodAttributes.Virtual,
            MethodModifiers = [(mb, ctx) => { }],
            Emitter = (il, ctx) => il.Emit(OpCodes.Ret)
        };

        Assert.Equal("Custom", method.Name);
        Assert.Equal(typeof(string), method.ReturnType);
        Assert.Equal([typeof(int)], method.Parameters);
        Assert.Equal(MethodAttributes.Public | MethodAttributes.Virtual, method.Attributes);
        Assert.Single(method.MethodModifiers);
        Assert.NotNull(method.Emitter);
    }
}
