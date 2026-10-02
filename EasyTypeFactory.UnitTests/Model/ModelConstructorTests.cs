using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Model;

public class ModelConstructorTests
{
    [Fact]
    public void Properties_AreCorrectlyInitialized()
    {
        var field = new ModelField
        {
            Name = "id",
            OriginalName = "Id",
            Type = typeof(int)
        };

        var constructor = new ModelConstructor
        {
            Fields = [field],
            SetsRequiredMembers = true
        };

        Assert.Single(constructor.Fields);
        Assert.Same(field, constructor.Fields[0]);
        Assert.True(constructor.SetsRequiredMembers);
    }
}
