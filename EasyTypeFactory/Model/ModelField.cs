using System;

namespace EasyTypeFactory.Model;

internal sealed class ModelField
{
    public required string Name { get; init; }
    
    public required string OriginalName { get; init; }
    
    public required Type Type { get; init; }
    
    public bool Required { get; init; }
    
    public bool ReadOnly { get; init; }
    
    public object? DefaultValue { get; init; }
    
    public bool Private { get; init; }
}