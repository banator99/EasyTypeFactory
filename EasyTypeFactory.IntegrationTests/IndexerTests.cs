using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace EasyTypeFactory.IntegrationTests;

public class IndexerTests
{
    [Fact]
    public void WithIndexer_GetterAndSetter_WorkCorrectly()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Id", typeof(int), defaultValue: 10)
            .AddField("Name", typeof(string), defaultValue: "Alice")
            .WithParameterlessConstructor()
            .WithIndexer(setter: true)
            .Build();

        var instance = Activator.CreateInstance(type)!;
        var indexer = type.GetProperty("Item")!;
        Assert.NotNull(indexer);
        Assert.NotNull(indexer.GetGetMethod());
        Assert.NotNull(indexer.GetSetMethod());

        // Get values
        Assert.Equal(10, indexer.GetValue(instance, ["Id"]));
        Assert.Equal("Alice", indexer.GetValue(instance, ["Name"]));

        // Set values
        indexer.SetValue(instance, 20, ["Id"]);
        indexer.SetValue(instance, "Bob", ["Name"]);

        Assert.Equal(20, indexer.GetValue(instance, ["Id"]));
        Assert.Equal("Bob", indexer.GetValue(instance, ["Name"]));
    }

    [Fact]
    public void WithIndexer_ReadOnly_OnlyEmitsGetter()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Title", typeof(string), defaultValue: "ReadOnlyIndexer")
            .WithParameterlessConstructor()
            .WithIndexer(setter: false)
            .Build();

        var instance = Activator.CreateInstance(type)!;
        var indexer = type.GetProperty("Item")!;
        Assert.NotNull(indexer);
        Assert.NotNull(indexer.GetGetMethod());
        Assert.Null(indexer.GetSetMethod());

        Assert.Equal("ReadOnlyIndexer", indexer.GetValue(instance, ["Title"]));
    }

    [Fact]
    public void WithoutIndexer_DoesNotEmitIndexer()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Title", typeof(string))
            .WithIndexer()
            .WithoutIndexer()
            .Build();

        var indexer = type.GetProperty("Item");
        Assert.Null(indexer);
    }

    [Fact]
    public void Indexer_CaseInsensitive_MatchesPropertiesCaseInsensitively()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("UserAge", typeof(int), defaultValue: 25)
            .WithParameterlessConstructor()
            .WithIndexer(setter: true)
            .CaseInsensitive()
            .Build();

        var instance = Activator.CreateInstance(type)!;
        var indexer = type.GetProperty("Item")!;

        Assert.Equal(25, indexer.GetValue(instance, ["userage"]));
        Assert.Equal(25, indexer.GetValue(instance, ["USERAGE"]));

        indexer.SetValue(instance, 35, ["userAGE"]);
        Assert.Equal(35, indexer.GetValue(instance, ["userage"]));
    }
}
