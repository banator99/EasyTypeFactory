using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Model;

public class ModelGetTByNameMethodTests
{
    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var method = new ModelGetTByNameMethod
        {
            Name = "GetByName"
        };
        Assert.Equal("GetByName", method.Name);
    }
}
