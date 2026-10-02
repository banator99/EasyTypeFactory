using System.Diagnostics.CodeAnalysis;

namespace EasyTypeFactory.Model;

internal sealed class ModelEqualsTypeMethod : ModelMethod
{
    [SetsRequiredMembers]
    public ModelEqualsTypeMethod()
    {
        Name = "Equals";
    }
}