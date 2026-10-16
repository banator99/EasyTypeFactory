using EasyTypeFactory.Utils;
using Xunit;

namespace EasyTypeFactory.UnitTests.Utils;

public class SharedTests
{
    [Fact]
    public void Constants_HaveExpectedValues()
    {
        Assert.Equal("EasyTypeFactory.Generated", Shared.DefaultNamespace);
        Assert.Equal("Anonymous", Shared.DefaultClassName);
        Assert.Equal("EasyTypeFactory.Generated.Anonymous", Shared.DefaultFullName);
    }

    [Fact]
    public void ModuleBuilder_ReturnsSameInstanceAndIsValid()
    {
        var mb1 = Shared.ModuleBuilder;
        var mb2 = Shared.ModuleBuilder;

        Assert.NotNull(mb1);
        Assert.Same(mb1, mb2);
        Assert.NotNull(mb1.Assembly);
        Assert.Equal(Shared.DefaultNamespace, mb1.Assembly.GetName().Name);
    }
}
