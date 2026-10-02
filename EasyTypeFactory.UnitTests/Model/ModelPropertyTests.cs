using System;
using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Model;

public class ModelPropertyTests
{
    [Fact]
    public void Properties_AreCorrectlyInitialized()
    {
        var field = new ModelField
        {
            Name = "_name",
            OriginalName = "Name",
            Type = typeof(string)
        };

        var prop = new ModelProperty
        {
            Name = "Name",
            Field = field,
            HasSet = true,
            HasInit = false,
            OverriddenGetMethod = null,
            OverriddenSetMethod = null,
            ExplicitImplementationType = typeof(IDisposable)
        };

        Assert.Equal("Name", prop.Name);
        Assert.Same(field, prop.Field);
        Assert.True(prop.HasSet);
        Assert.False(prop.HasInit);
        Assert.Null(prop.OverriddenGetMethod);
        Assert.Null(prop.OverriddenSetMethod);
        Assert.Equal(typeof(IDisposable), prop.ExplicitImplementationType);
    }
}
