using System;

namespace EasyTypeFactory.Definition;

internal record FieldDefinition(
    Type Type,
    bool ReadOnly,
    bool Required,
    object? DefaultValue)
{
    public virtual bool Equals(FieldDefinition? other)
    {
        if (other is null)
        {
            return false;
        }
        
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        
        return Type == other.Type &&
               ReadOnly == other.ReadOnly &&
               Required == other.Required &&
               Equals(DefaultValue, other.DefaultValue);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, ReadOnly, Required, DefaultValue);
    }
}
