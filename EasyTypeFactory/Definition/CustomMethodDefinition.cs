using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Utils;

namespace EasyTypeFactory.Definition;

internal record CustomMethodDefinition(
    string Name,
    MethodAttributes Attributes,
    Type? ReturnType,
    Type[]? ParameterTypes,
    ImmutableList<Action<MethodBuilder, MethodBuilderContext>> Modifiers,
    Action<ILGenerator, MethodBuilderContext>? Emitter)
{
    public CustomMethodDefinition(string name) : this(
        Name: name,
        Attributes: MethodAttributes.Public,
        ReturnType: null,
        ParameterTypes: [],
        Modifiers: [],
        Emitter: null )
    {
    }
    
    public virtual bool Equals(CustomMethodDefinition? other)
    {
        if (other is null)
        {
            return false;
        }
        
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        
        return Name == other.Name &&
               Attributes == other.Attributes &&
               ReturnType == other.ReturnType &&
               (
                   (
                       ParameterTypes == null &&
                       other.ParameterTypes == null
                    ) || (
                       ParameterTypes != null && 
                       other.ParameterTypes != null &&
                       ParameterTypes.SequenceEqual(other.ParameterTypes)
                    )
               ) &&
               Modifiers.SequenceEqual(other.Modifiers) &&
               Emitter == other.Emitter;
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();

        hashCode.Add(Name);
        hashCode.Add(Attributes);
        hashCode.Add(ReturnType);
        hashCode.AddCollection(ParameterTypes);
        hashCode.AddCollection(Modifiers);
        hashCode.Add(Emitter);
        
        return hashCode.ToHashCode();
    }
}
