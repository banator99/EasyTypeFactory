using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Model;

public class ModelGetHashCodeMethodTests
{
    [Fact]
    public void Constructor_SetsDefaultName()
    {
        var method = new ModelGetHashCodeMethod();
        Assert.Equal("GetHashCode", method.Name);
    }
}
