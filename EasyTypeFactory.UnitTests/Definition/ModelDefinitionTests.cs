using System;
using System.Collections.Immutable;
using System.Reflection.Emit;
using EasyTypeFactory.Definition;
using EasyTypeFactory.Utils;
using Xunit;

namespace EasyTypeFactory.UnitTests.Definition;

public class ModelDefinitionTests
{
    [Fact]
    public void Constructor_Default_InitializesDefaultValues()
    {
        var def = new ModelDefinition();
        Assert.Null(def.Name);
        Assert.True(def.DiscriminateName);
        Assert.Null(def.ModuleBuilder);
        Assert.Null(def.BaseType);
        Assert.Empty(def.Fields);
        Assert.Empty(def.Interfaces);
        Assert.Null(def.GetByName);
        Assert.Null(def.GetTByName);
        Assert.Empty(def.GetTypeByName);
        Assert.False(def.AddAllGetTypeByNameMethods);
        Assert.Null(def.SetByName);
        Assert.Null(def.SetTByName);
        Assert.Empty(def.SetTypeByName);
        Assert.False(def.AddAllSetTypeByNameMethods);
        Assert.False(def.IndexerGet);
        Assert.False(def.IndexerSet);
        Assert.Empty(def.CustomMethods);
        Assert.False(def.ParameterlessConstructor);
        Assert.False(def.MinimalConstructor);
        Assert.False(def.FullConstructor);
        Assert.False(def.Equality);
        Assert.False(def.EqualityOperators);
        Assert.False(def.CaseInsensitive);
    }

    [Fact]
    public void Equals_And_GetHashCode_WorkCorrectly()
    {
        var def1 = new ModelDefinition();
        var def2 = new ModelDefinition();

        Assert.True(def1.Equals(def2));
        Assert.True(def1.Equals((object)def2));
        Assert.True(def1.Equals(def1));
        Assert.False(def1.Equals(null));
        Assert.False(def1.Equals((object?)null));
        Assert.False(def1.Equals("not model definition"));

        Assert.Equal(def1.GetHashCode(), def2.GetHashCode());

        var customMethod = new CustomMethodDefinition("Foo");
        var field = new FieldDefinition(typeof(int), false, false, null);

        var defFull = def1 with
        {
            Name = "MyName",
            DiscriminateName = false,
            ModuleBuilder = Shared.ModuleBuilder,
            BaseType = typeof(object),
            Fields = ImmutableDictionary<string, FieldDefinition>.Empty.Add("f1", field),
            Interfaces = ImmutableHashSet<Type>.Empty.Add(typeof(IDisposable)),
            GetByName = "GetVal",
            GetTByName = "GetTVal",
            GetTypeByName = ImmutableDictionary<Type, string>.Empty.Add(typeof(int), "GetInt"),
            AddAllGetTypeByNameMethods = true,
            SetByName = "SetVal",
            SetTByName = "SetTVal",
            SetTypeByName = ImmutableDictionary<Type, string>.Empty.Add(typeof(int), "SetInt"),
            AddAllSetTypeByNameMethods = true,
            IndexerGet = true,
            IndexerSet = true,
            CustomMethods = ImmutableList<CustomMethodDefinition>.Empty.Add(customMethod),
            ParameterlessConstructor = true,
            MinimalConstructor = true,
            FullConstructor = true,
            Equality = true,
            EqualityOperators = true,
            CaseInsensitive = true
        };

        var defFull2 = def1 with
        {
            Name = "MyName",
            DiscriminateName = false,
            ModuleBuilder = Shared.ModuleBuilder,
            BaseType = typeof(object),
            Fields = ImmutableDictionary<string, FieldDefinition>.Empty.Add("f1", field),
            Interfaces = ImmutableHashSet<Type>.Empty.Add(typeof(IDisposable)),
            GetByName = "GetVal",
            GetTByName = "GetTVal",
            GetTypeByName = ImmutableDictionary<Type, string>.Empty.Add(typeof(int), "GetInt"),
            AddAllGetTypeByNameMethods = true,
            SetByName = "SetVal",
            SetTByName = "SetTVal",
            SetTypeByName = ImmutableDictionary<Type, string>.Empty.Add(typeof(int), "SetInt"),
            AddAllSetTypeByNameMethods = true,
            IndexerGet = true,
            IndexerSet = true,
            CustomMethods = ImmutableList<CustomMethodDefinition>.Empty.Add(customMethod),
            ParameterlessConstructor = true,
            MinimalConstructor = true,
            FullConstructor = true,
            Equality = true,
            EqualityOperators = true,
            CaseInsensitive = true
        };

        Assert.True(defFull.Equals(defFull2));
        Assert.Equal(defFull.GetHashCode(), defFull2.GetHashCode());

        // Test variations for DictionaryEquals differences
        Assert.False(defFull.Equals(defFull with { Name = "Other" }));
        Assert.False(defFull.Equals(defFull with { DiscriminateName = true }));
        Assert.False(defFull.Equals(defFull with { ModuleBuilder = null }));
        Assert.False(defFull.Equals(defFull with { BaseType = null }));
        Assert.False(defFull.Equals(defFull with { Fields = ImmutableDictionary<string, FieldDefinition>.Empty }));
        Assert.False(defFull.Equals(defFull with { Fields = defFull.Fields.SetItem("f1", new FieldDefinition(typeof(string), false, false, null)) }));
        Assert.False(defFull.Equals(defFull with { Interfaces = ImmutableHashSet<Type>.Empty }));
        Assert.False(defFull.Equals(defFull with { GetByName = "OtherGet" }));
        Assert.False(defFull.Equals(defFull with { GetTByName = "OtherGetT" }));
        Assert.False(defFull.Equals(defFull with { GetTypeByName = ImmutableDictionary<Type, string>.Empty }));
        Assert.False(defFull.Equals(defFull with { GetTypeByName = defFull.GetTypeByName.SetItem(typeof(int), "Other") }));
        Assert.False(defFull.Equals(defFull with { AddAllGetTypeByNameMethods = false }));
        Assert.False(defFull.Equals(defFull with { SetByName = "OtherSet" }));
        Assert.False(defFull.Equals(defFull with { SetTByName = "OtherSetT" }));
        Assert.False(defFull.Equals(defFull with { SetTypeByName = ImmutableDictionary<Type, string>.Empty }));
        Assert.False(defFull.Equals(defFull with { SetTypeByName = defFull.SetTypeByName.SetItem(typeof(int), "Other") }));
        Assert.False(defFull.Equals(defFull with { AddAllSetTypeByNameMethods = false }));
        Assert.False(defFull.Equals(defFull with { IndexerGet = false }));
        Assert.False(defFull.Equals(defFull with { IndexerSet = false }));
        Assert.False(defFull.Equals(defFull with { CustomMethods = ImmutableList<CustomMethodDefinition>.Empty }));
        Assert.False(defFull.Equals(defFull with { ParameterlessConstructor = false }));
        Assert.False(defFull.Equals(defFull with { MinimalConstructor = false }));
        Assert.False(defFull.Equals(defFull with { FullConstructor = false }));
        Assert.False(defFull.Equals(defFull with { Equality = false }));
        Assert.False(defFull.Equals(defFull with { EqualityOperators = false }));
        Assert.False(defFull.Equals(defFull with { CaseInsensitive = false }));
    }
}
