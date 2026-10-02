namespace EasyTypeFactory.Model;

internal sealed class ModelConstructor
{
    public required ModelField[] Fields { get; init; }
    
    public bool SetsRequiredMembers { get; init; }
}
