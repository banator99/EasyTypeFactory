using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Model;

public class ModelSetTByNameMethodTests
{
    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var method = new ModelSetTByNameMethod
        {
            Name = "SetByName"
        };
        Assert.Equal("SetByName", method.Name);
    }
}
