using System;
using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Model;

public class ModelFieldTests
{
    [Fact]
    public void Properties_AreCorrectlyInitialized()
    {
        var field = new ModelField
        {
            Name = "_field",
            OriginalName = "Field",
            Type = typeof(string),
            Required = true,
            ReadOnly = true,
            DefaultValue = "Default",
            Private = true
        };

        Assert.Equal("_field", field.Name);
        Assert.Equal("Field", field.OriginalName);
        Assert.Equal(typeof(string), field.Type);
        Assert.True(field.Required);
        Assert.True(field.ReadOnly);
        Assert.Equal("Default", field.DefaultValue);
        Assert.True(field.Private);
    }
}
