using System;
using EasyTypeFactory.Definition;
using Xunit;

namespace EasyTypeFactory.UnitTests.Definition;

public class FieldDefinitionTests
{
    [Fact]
    public void Properties_AreCorrectlyInitialized()
    {
        var def = new FieldDefinition(typeof(int), ReadOnly: true, Required: true, DefaultValue: 42);
        Assert.Equal(typeof(int), def.Type);
        Assert.True(def.ReadOnly);
        Assert.True(def.Required);
        Assert.Equal(42, def.DefaultValue);
    }

    [Fact]
    public void Equals_And_GetHashCode_WorkCorrectly()
    {
        var fd1 = new FieldDefinition(typeof(int), true, false, 10);
        var fd2 = new FieldDefinition(typeof(int), true, false, 10);
        var fd3 = new FieldDefinition(typeof(string), true, false, 10);
        var fd4 = new FieldDefinition(typeof(int), false, false, 10);
        var fd5 = new FieldDefinition(typeof(int), true, true, 10);
        var fd6 = new FieldDefinition(typeof(int), true, false, 20);

        Assert.True(fd1.Equals(fd2));
        Assert.True(fd1.Equals((object)fd2));
        Assert.True(fd1.Equals(fd1));
        Assert.False(fd1.Equals(null));
        Assert.False(fd1.Equals((object?)null));
        Assert.False(fd1.Equals("not a field definition"));

        Assert.False(fd1.Equals(fd3));
        Assert.False(fd1.Equals(fd4));
        Assert.False(fd1.Equals(fd5));
        Assert.False(fd1.Equals(fd6));

        Assert.Equal(fd1.GetHashCode(), fd2.GetHashCode());
    }
}
