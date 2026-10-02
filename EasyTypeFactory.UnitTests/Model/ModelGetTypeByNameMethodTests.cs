using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Model;

public class ModelGetTypeByNameMethodTests
{
    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var method = new ModelGetTypeByNameMethod
        {
            Name = "GetStringByName",
            ReturnType = typeof(string)
        };
        Assert.Equal("GetStringByName", method.Name);
        Assert.Equal(typeof(string), method.ReturnType);
    }
}
