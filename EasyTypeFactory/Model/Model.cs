using System;

namespace EasyTypeFactory.Model;

internal sealed class Model
{
    public required string? Name { get; init; }
    
    public required bool DiscriminateName { get; init; }
    
    public required ModelField[] Fields { get; init; }
    
    public required ModelProperty[] Properties { get; init; }
    
    public required ModelIndexer? Indexer { get; init; }
    
    public required ModelConstructor[] Constructors { get; init; }
    
    public required Type[] Interfaces { get; init; }

    public required Type? BaseType { get; init; }

    public required ModelMethod[] Methods { get; init; }
    
    public required ModelOperator[] Operators { get; init; }

    public required bool CaseInsensitive { get; init; }
}
