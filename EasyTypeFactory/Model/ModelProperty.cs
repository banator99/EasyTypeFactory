using System;
using System.Reflection;

namespace EasyTypeFactory.Model;

internal sealed class ModelProperty
{
    public required string Name { get; init; }
    
    public required ModelField Field { get; init; }
    
    public bool HasSet { get; init; }
    
    public bool HasInit { get; init; }
    
    public MethodInfo? OverriddenGetMethod { get; init; }
    
    public MethodInfo? OverriddenSetMethod { get; init; }
    
    public Type? ExplicitImplementationType { get; init; }
}