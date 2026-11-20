using EasyTypeFactory.Model;

namespace EasyTypeFactory.UnitTests.Model;

public class ModelSetByNameMethodTests
{
    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var method = new ModelSetByNameMethod
        {
            Name = "SetByName"
        };
        Assert.Equal("SetByName", method.Name);
    }
}
