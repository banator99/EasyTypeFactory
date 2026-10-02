using System.Reflection;

namespace EasyTypeFactory.Model;

internal sealed class ModelIndexer
{
    public bool HasSet { get; init; } 
    
    public MethodInfo? OverriddenGetMethod { get; init; }
    
    public MethodInfo? OverriddenSetMethod { get; init; }
}
