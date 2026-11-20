using EasyTypeFactory.Model;

namespace EasyTypeFactory.UnitTests.Model;

public class ModelIndexerTests
{
    [Fact]
    public void Properties_AreCorrectlyInitialized()
    {
        var indexer = new ModelIndexer
        {
            HasSet = true,
            OverriddenGetMethod = null,
            OverriddenSetMethod = null
        };

        Assert.True(indexer.HasSet);
        Assert.Null(indexer.OverriddenGetMethod);
        Assert.Null(indexer.OverriddenSetMethod);
    }
}
