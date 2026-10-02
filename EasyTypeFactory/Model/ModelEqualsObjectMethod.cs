using System.Diagnostics.CodeAnalysis;

namespace EasyTypeFactory.Model;

internal sealed class ModelEqualsObjectMethod : ModelMethod
{
    [SetsRequiredMembers]
    public ModelEqualsObjectMethod()
    {
        Name = "Equals";
    }
}