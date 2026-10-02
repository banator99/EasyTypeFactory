using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Model;

public class ModelOperatorTests
{
    [Fact]
    public void Operators_CanBeInstantiated()
    {
        var eq = new ModelEqualityOperator();
        var ineq = new ModelInequalityOperator();

        Assert.IsAssignableFrom<ModelOperator>(eq);
        Assert.IsAssignableFrom<ModelOperator>(ineq);
    }
}
