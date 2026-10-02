using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Definition;
using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Model;

public class ModelBuilderTests
{
    public interface ITestEntity
    {
        int Id { get; set; }
        string Tag { get; }
    }

    public interface ISubTestEntity : ITestEntity
    {
        string Description { get; set; }
    }

    public abstract class BaseEntity
    {
        public abstract int BaseId { get; set; }
        public abstract string BaseName { get; }
        public abstract void AbstractMethod();
    }

    public interface IWithIndexer
    {
        object this[string key] { get; set; }
    }

    [Fact]
    public void Create_WithDefaults_BuildsExpectedModel()
    {
        var def = new ModelDefinition
        {
            Name = "MyTestModel",
            DiscriminateName = true,
            Fields = ImmutableDictionary<string, FieldDefinition>.Empty
                .Add("Id", new FieldDefinition(typeof(int), false, true, null))
                .Add("Name", new FieldDefinition(typeof(string), true, false, "DefaultName"))
                .Add("City", new FieldDefinition(typeof(string), false, false, "Amsterdam")),
            ParameterlessConstructor = true,
            MinimalConstructor = true,
            FullConstructor = true,
            Equality = true,
            EqualityOperators = true,
            GetByName = "GetVal",
            GetTByName = "GetTVal",
            GetTypeByName = ImmutableDictionary<Type, string>.Empty.Add(typeof(int), "GetInt"),
            AddAllGetTypeByNameMethods = true,
            SetByName = "SetVal",
            SetTByName = "SetTVal",
            SetTypeByName = ImmutableDictionary<Type, string>.Empty.Add(typeof(int), "SetInt"),
            AddAllSetTypeByNameMethods = true,
            IndexerGet = true,
            IndexerSet = true
        };

        var model = ModelBuilder.Create(def);

        Assert.NotNull(model);
        Assert.Equal("MyTestModel", model.Name);
        Assert.True(model.DiscriminateName);
        Assert.Equal(3, model.Fields.Length);
        Assert.Empty(model.Properties);
        Assert.NotNull(model.Indexer);
        Assert.True(model.Indexer.HasSet);
        Assert.Equal(3, model.Constructors.Length);
        Assert.Equal(2, model.Operators.Length); // == and !=
        Assert.Contains(model.Methods, m => m is ModelGetByNameMethod);
        Assert.Contains(model.Methods, m => m is ModelGetTByNameMethod);
        Assert.Contains(model.Methods, m => m is ModelGetTypeByNameMethod);
        Assert.Contains(model.Methods, m => m is ModelSetByNameMethod);
        Assert.Contains(model.Methods, m => m is ModelSetTByNameMethod);
        Assert.Contains(model.Methods, m => m is ModelSetTypeByNameMethod);
        Assert.Contains(model.Methods, m => m is ModelEqualsObjectMethod);
        Assert.Contains(model.Methods, m => m is ModelEqualsTypeMethod);
        Assert.Contains(model.Methods, m => m is ModelGetHashCodeMethod);
    }

    [Fact]
    public void Create_WithInterfacesAndInheritance_ScansPropertiesAndHierarchy()
    {
        var def = new ModelDefinition
        {
            Interfaces = ImmutableHashSet<Type>.Empty.Add(typeof(ISubTestEntity)),
            Fields = ImmutableDictionary<string, FieldDefinition>.Empty
                .Add("Id", new FieldDefinition(typeof(int), false, false, null))
                .Add("Tag", new FieldDefinition(typeof(string), true, false, "TagVal"))
                .Add("Description", new FieldDefinition(typeof(string), false, false, null))
        };

        var model = ModelBuilder.Create(def);
        Assert.NotNull(model);
        Assert.Contains(typeof(ISubTestEntity), model.Interfaces);
        Assert.Equal(3, model.Properties.Length);
    }

    [Fact]
    public void Create_WithBaseClass_ScansAbstractPropertiesAndMethods()
    {
        var def = new ModelDefinition
        {
            BaseType = typeof(BaseEntity),
            Fields = ImmutableDictionary<string, FieldDefinition>.Empty
                .Add("BaseId", new FieldDefinition(typeof(int), false, false, null))
                .Add("BaseName", new FieldDefinition(typeof(string), true, false, "NameVal")),
            CustomMethods = ImmutableList<CustomMethodDefinition>.Empty.Add(
                new CustomMethodDefinition("AbstractMethod", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), Type.EmptyTypes, ImmutableList<Action<MethodBuilder, MethodBuilderContext>>.Empty, (il, ctx) => il.Emit(OpCodes.Ret)))
        };

        var model = ModelBuilder.Create(def);
        Assert.NotNull(model);
        Assert.Equal(typeof(BaseEntity), model.BaseType);
        Assert.Equal(2, model.Properties.Length);
        Assert.Contains(model.Methods, m => m is ModelCustomMethod);
    }

    [Fact]
    public void Create_WithInterfaceAsBaseType_TreatsAsInterface()
    {
        var def = new ModelDefinition
        {
            BaseType = typeof(ITestEntity),
            Fields = ImmutableDictionary<string, FieldDefinition>.Empty
                .Add("Id", new FieldDefinition(typeof(int), false, false, null))
                .Add("Tag", new FieldDefinition(typeof(string), true, false, "TagVal"))
        };

        var model = ModelBuilder.Create(def);
        Assert.NotNull(model);
        Assert.Contains(typeof(ITestEntity), model.Interfaces);
    }

    [Fact]
    public void Create_WithInterfaceIndexer_ScansIndexer()
    {
        var def = new ModelDefinition
        {
            Interfaces = ImmutableHashSet<Type>.Empty.Add(typeof(IWithIndexer)),
            IndexerGet = true,
            IndexerSet = true,
            Fields = ImmutableDictionary<string, FieldDefinition>.Empty
                .Add("Field1", new FieldDefinition(typeof(string), false, false, null))
        };

        var model = ModelBuilder.Create(def);
        Assert.NotNull(model.Indexer);
        Assert.True(model.Indexer.HasSet);
    }
}
