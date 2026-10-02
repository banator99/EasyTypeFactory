using System.Diagnostics.CodeAnalysis;

namespace EasyTypeFactory.Model;

internal sealed class ModelGetHashCodeMethod : ModelMethod
{
    [SetsRequiredMembers]
    public ModelGetHashCodeMethod()
    {
        Name = "GetHashCode";
    }
}