using System.Reflection.Emit;

namespace EasyTypeFactory;

public class MethodBuilderContext
{
    public MethodBuilder MethodBuilder { get; }

    internal MethodBuilderContext(MethodBuilder methodBuilder)
    {
        MethodBuilder = methodBuilder;
    }
}
