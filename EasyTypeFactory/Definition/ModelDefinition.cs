using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Emit;
using EasyTypeFactory.Utils;

namespace EasyTypeFactory.Definition;

internal record ModelDefinition(
    string? Name,
    bool DiscriminateName,
    ModuleBuilder? ModuleBuilder,
    Type? BaseType,
    ImmutableDictionary<string, FieldDefinition> Fields,
    ImmutableHashSet<Type> Interfaces,
    string? GetByName,
    string? GetTByName,
    ImmutableDictionary<Type, string> GetTypeByName,
    bool AddAllGetTypeByNameMethods,
    string? SetByName,
    string? SetTByName,
    ImmutableDictionary<Type, string> SetTypeByName,
    bool AddAllSetTypeByNameMethods,
    bool IndexerGet,
    bool IndexerSet,
    ImmutableList<CustomMethodDefinition> CustomMethods,
    bool ParameterlessConstructor,
    bool MinimalConstructor,
    bool FullConstructor,
    bool Equality,
    bool EqualityOperators,
    bool CaseInsensitive)
{
    public ModelDefinition() : this(
        Name: null,
        DiscriminateName: true,
        ModuleBuilder: null,
        BaseType: null,
        Fields: ImmutableDictionary<string, FieldDefinition>.Empty,
        Interfaces: ImmutableHashSet<Type>.Empty,
        GetByName: null,
        GetTByName: null,
        GetTypeByName: ImmutableDictionary<Type, string>.Empty,
        AddAllGetTypeByNameMethods: false,
        SetByName: null,
        SetTByName: null,
        SetTypeByName: ImmutableDictionary<Type, string>.Empty,
        AddAllSetTypeByNameMethods: false,
        IndexerGet: false,
        IndexerSet: false,
        CustomMethods: ImmutableList<CustomMethodDefinition>.Empty,
        ParameterlessConstructor: false,
        MinimalConstructor: false,
        FullConstructor: false,
        Equality: false,
        EqualityOperators: false,
        CaseInsensitive: false)
    {
    }
    
    public virtual bool Equals(ModelDefinition? other)
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
               DiscriminateName == other.DiscriminateName &&
               ModuleBuilder == other.ModuleBuilder &&
               BaseType == other.BaseType &&
               DictionaryEquals(Fields, other.Fields) &&
               Interfaces.SequenceEqual(other.Interfaces) && 
               GetByName == other.GetByName && 
               GetTByName == other.GetTByName && 
               DictionaryEquals(GetTypeByName, other.GetTypeByName) && 
               AddAllGetTypeByNameMethods == other.AddAllGetTypeByNameMethods &&
               SetByName == other.SetByName &&
               SetTByName == other.SetTByName &&
               DictionaryEquals(SetTypeByName, other.SetTypeByName) &&
               AddAllSetTypeByNameMethods == other.AddAllSetTypeByNameMethods &&
               IndexerGet == other.IndexerGet && 
               IndexerSet == other.IndexerSet &&
               CustomMethods.SequenceEqual(other.CustomMethods) &&
               ParameterlessConstructor == other.ParameterlessConstructor &&
               MinimalConstructor == other.MinimalConstructor &&
               FullConstructor == other.FullConstructor &&
               Equality == other.Equality &&
               EqualityOperators == other.EqualityOperators &&
               CaseInsensitive == other.CaseInsensitive;
    }

    private static bool DictionaryEquals<TKey, TValue>(
        ImmutableDictionary<TKey, TValue> left,
        ImmutableDictionary<TKey, TValue> right)
        where TKey : notnull
    {
        if (left.Count != right.Count)
        {
            return false;
        }
        
        foreach (var (key, value) in left)
        {
            if (!right.TryGetValue(key, out var rightValue) || !Equals(value, rightValue))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        
        hashCode.Add(Name);
        hashCode.Add(DiscriminateName);
        hashCode.Add(ModuleBuilder);
        hashCode.Add(BaseType);
        hashCode.AddCollection(Fields);
        hashCode.AddCollection(Interfaces);
        hashCode.Add(GetByName);
        hashCode.Add(GetTByName);
        hashCode.AddDictionary(GetTypeByName);
        hashCode.Add(AddAllGetTypeByNameMethods);
        hashCode.Add(SetByName);
        hashCode.Add(SetTByName);
        hashCode.AddDictionary(SetTypeByName);
        hashCode.Add(AddAllSetTypeByNameMethods);
        hashCode.Add(IndexerGet);
        hashCode.Add(IndexerSet);
        hashCode.AddCollection(CustomMethods);
        hashCode.Add(ParameterlessConstructor);
        hashCode.Add(MinimalConstructor);
        hashCode.Add(FullConstructor);
        hashCode.Add(Equality);
        hashCode.Add(EqualityOperators);
        hashCode.Add(CaseInsensitive);
        
        return hashCode.ToHashCode();
    }
}
