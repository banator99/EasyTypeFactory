using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Model;

public class ModelEqualsObjectMethodTests
{
    [Fact]
    public void Constructor_SetsDefaultName()
    {
        var method = new ModelEqualsObjectMethod();
        Assert.Equal("Equals", method.Name);
    }
}
