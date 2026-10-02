using System;
using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Model;

public class ModelTests
{
    [Fact]
    public void Properties_AreCorrectlyInitialized()
    {
        var model = new global::EasyTypeFactory.Model.Model
        {
            Name = "MyModel",
            DiscriminateName = true,
            Fields = [],
            Properties = [],
            Indexer = null,
            Constructors = [],
            Interfaces = [typeof(IDisposable)],
            BaseType = typeof(object),
            Methods = [],
            Operators = [],
            CaseInsensitive = true
        };

        Assert.Equal("MyModel", model.Name);
        Assert.True(model.DiscriminateName);
        Assert.Empty(model.Fields);
        Assert.Empty(model.Properties);
        Assert.Null(model.Indexer);
        Assert.Empty(model.Constructors);
        Assert.Equal([typeof(IDisposable)], model.Interfaces);
        Assert.Equal(typeof(object), model.BaseType);
        Assert.Empty(model.Methods);
        Assert.Empty(model.Operators);
        Assert.True(model.CaseInsensitive);
    }
}
