using System;
using System.Reflection.Emit;
using Xunit;

namespace EasyTypeFactory.IntegrationTests;

public class EasyTypeBuilderTests
{
    [Fact]
    public void Create_ReturnsNewBuilderWithDefaultName()
    {
        var builder = EasyTypeBuilder.Create();
        Assert.NotNull(builder);

        var type = builder.Build();
        Assert.NotNull(type);
        Assert.StartsWith("EasyTypeFactory.Generated.DynamicModel", type.FullName!);
    }

    [Fact]
    public void Create_WithCustomName_UsesGivenName()
    {
        var customName = "MyCompany.CustomNamespace.MyCustomType";
        var builder = EasyTypeBuilder.Create(customName, discriminate: false);
        var type = builder.Build();

        Assert.Equal(customName, type.FullName);
    }

    [Fact]
    public void WithName_Discriminated_AppendsUniqueDiscriminator()
    {
        var baseName = "TestNamespace.DiscriminatedType";
        var builder = EasyTypeBuilder.Create().WithName(baseName, discriminate: true);
        var type = builder.Build();

        Assert.StartsWith(baseName + "<", type.FullName!);
        Assert.NotEqual(baseName, type.FullName);
    }

    [Fact]
    public void WithName_NotDiscriminated_KeepsExactName()
    {
        var exactName = "TestNamespace.ExactTypeName";
        var builder = EasyTypeBuilder.Create().WithName(exactName, discriminate: false);
        var type = builder.Build();

        Assert.Equal(exactName, type.FullName);
    }

    [Fact]
    public void WithoutName_ResetsNameToDefault()
    {
        var builder = EasyTypeBuilder.Create("CustomName", discriminate: false)
            .WithoutName();
        var type = builder.Build();

        Assert.StartsWith("EasyTypeFactory.Generated.DynamicModel", type.FullName!);
    }

    [Fact]
    public void Defaults_AppliesDefaultConfigurations()
    {
        var builder = EasyTypeBuilder.Create()
            .AddField("Id", typeof(int))
            .Defaults();

        var type = builder.Build();
        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);

        // Defaults enable parameterless, minimal and full constructors and equality
        var idField = type.GetField("Id");
        Assert.NotNull(idField);

        var parameterlessCtor = type.GetConstructor(Type.EmptyTypes);
        Assert.NotNull(parameterlessCtor);

        var fullCtor = type.GetConstructor([typeof(int)]);
        Assert.NotNull(fullCtor);

        var equalsMethod = type.GetMethod("Equals", [typeof(object)]);
        Assert.NotNull(equalsMethod);
    }

    [Fact]
    public void WithModuleBuilder_And_WithoutModuleBuilder_WorksCorrectly()
    {
        var assemblyName = new System.Reflection.AssemblyName("CustomTestDynamicAssembly");
        var dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var customModule = dynamicAssembly.DefineDynamicModule("CustomTestDynamicModule");

        var builder = EasyTypeBuilder.Create("TestCustomModuleType")
            .WithModuleBuilder(customModule);

        var type = builder.Build();
        Assert.Equal(customModule.Assembly.FullName, type.Assembly.FullName);

        var builderWithout = builder.WithoutModuleBuilder();
        var typeWithout = builderWithout.Build();
        Assert.NotNull(typeWithout);
    }

    [Fact]
    public void Builder_Immutability_CreatesNewInstances()
    {
        var builder1 = EasyTypeBuilder.Create("TypeA", discriminate: false);
        var builder2 = builder1.WithName("TypeB", discriminate: false);

        Assert.NotSame(builder1, builder2);
        Assert.Equal("TypeA", builder1.Build().FullName);
        Assert.Equal("TypeB", builder2.Build().FullName);
    }

    [Fact]
    public void Builder_EqualityAndHashCode_WorkCorrectly()
    {
        var builder1 = (EasyTypeBuilder)EasyTypeBuilder.Create("SameType")
            .AddField("Field1", typeof(string))
            .WithEquality();

        var builder2 = (EasyTypeBuilder)EasyTypeBuilder.Create("SameType")
            .AddField("Field1", typeof(string))
            .WithEquality();

        var builder3 = (EasyTypeBuilder)EasyTypeBuilder.Create("DifferentType")
            .AddField("Field1", typeof(string));

        Assert.True(builder1.Equals(builder2));
        Assert.True(builder1.Equals((object)builder2));
        Assert.False(builder1.Equals(builder3));
        Assert.False(builder1.Equals(null));
        Assert.False(builder1.Equals("some string"));

        Assert.Equal(builder1.GetHashCode(), builder2.GetHashCode());
        Assert.True(builder1 == builder2);
        Assert.False(builder1 != builder2);

        Assert.False(builder1 == builder3);
        Assert.True(builder1 != builder3);

        EasyTypeBuilder? nullBuilder1 = null;
        EasyTypeBuilder? nullBuilder2 = null;
        Assert.True(nullBuilder1 == nullBuilder2);
        Assert.False(nullBuilder1 != nullBuilder2);
        Assert.False(builder1 == nullBuilder1);
        Assert.True(builder1 != nullBuilder1);
    }
}
