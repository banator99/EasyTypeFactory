using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Model;

public class ModelEqualsTypeMethodTests
{
    [Fact]
    public void Constructor_SetsDefaultName()
    {
        var method = new ModelEqualsTypeMethod();
        Assert.Equal("Equals", method.Name);
    }
}
