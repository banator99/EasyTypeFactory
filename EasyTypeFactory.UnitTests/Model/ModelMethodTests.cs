using System;
using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Model;

public class ModelMethodTests
{
    private class TestMethod : ModelMethod;

    [Fact]
    public void Properties_AreCorrectlyInitialized()
    {
        var method = new TestMethod
        {
            Name = "DoWork",
            ReturnType = typeof(int),
            Parameters = [typeof(string)],
            OverriddenMethod = null
        };

        Assert.Equal("DoWork", method.Name);
        Assert.Equal(typeof(int), method.ReturnType);
        Assert.Equal([typeof(string)], method.Parameters);
        Assert.Null(method.OverriddenMethod);
    }
}
