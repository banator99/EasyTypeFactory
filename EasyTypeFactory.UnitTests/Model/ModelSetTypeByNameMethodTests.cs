using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Model;

public class ModelSetTypeByNameMethodTests
{
    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var method = new ModelSetTypeByNameMethod
        {
            Name = "SetStringByName",
            Parameters = [typeof(string), typeof(string)]
        };
        Assert.Equal("SetStringByName", method.Name);
        Assert.Equal([typeof(string), typeof(string)], method.Parameters);
    }
}
