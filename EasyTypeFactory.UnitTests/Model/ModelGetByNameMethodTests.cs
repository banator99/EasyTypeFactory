using EasyTypeFactory.Model;

namespace EasyTypeFactory.UnitTests.Model;

public class ModelGetByNameMethodTests
{
    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var method = new ModelGetByNameMethod
        {
            Name = "GetByName"
        };
        Assert.Equal("GetByName", method.Name);
    }
}
