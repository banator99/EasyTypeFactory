using System;
using System.Reflection;

namespace EasyTypeFactory.Model;

public abstract class ModelMethod
{
    public required string Name { get; init; }

    public Type ReturnType { get; init; } = typeof(void);
    
    public Type[]? Parameters { get; init; }
    
    public MethodInfo? OverriddenMethod { get; set; }
}
