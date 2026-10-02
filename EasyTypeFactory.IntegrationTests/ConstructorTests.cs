using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Xunit;

namespace EasyTypeFactory.IntegrationTests;

public class ConstructorTests
{
    [Fact]
    public void ParameterlessConstructor_CreatesAndInitializesDefaultValues()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Name", typeof(string), defaultValue: "John Doe")
            .AddField("Age", typeof(int), defaultValue: 30)
            .WithParameterlessConstructor()
            .Build();

        var constructor = type.GetConstructor(Type.EmptyTypes);
        Assert.NotNull(constructor);

        var instance = constructor.Invoke(null);
        Assert.NotNull(instance);

        Assert.Equal("John Doe", type.GetField("Name")!.GetValue(instance));
        Assert.Equal(30, type.GetField("Age")!.GetValue(instance));
    }

    [Fact]
    public void MinimalConstructor_AcceptsRequiredAndNonDefaultFields()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Id", typeof(int), isRequired: true)
            .AddField("Name", typeof(string), isReadOnly: true)
            .AddField("City", typeof(string), defaultValue: "Amsterdam")
            .WithMinimalConstructor()
            .Build();

        // Minimal constructor should have parameters for Id and Name, but not City (since City has a default value)
        var constructor = type.GetConstructors().Single(c => c.GetParameters().Length == 2);
        Assert.NotNull(constructor);

        var paramMap = new Dictionary<string, object>
        {
            ["Id"] = 101,
            ["Name"] = "Alice"
        };
        var instance = constructor.Invoke(constructor.GetParameters().Select(p => paramMap[p.Name!]).ToArray());
        Assert.NotNull(instance);

        Assert.Equal(101, type.GetField("Id")!.GetValue(instance));
        Assert.Equal("Alice", type.GetField("Name")!.GetValue(instance));
        Assert.Equal("Amsterdam", type.GetField("City")!.GetValue(instance));
    }

    [Fact]
    public void MinimalConstructor_WithRequiredMembers_SetsRequiredMembersAttribute()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Id", typeof(int), isRequired: true)
            .WithMinimalConstructor()
            .Build();

        var constructor = type.GetConstructor([typeof(int)]);
        Assert.NotNull(constructor);

        var setsRequiredMembers = constructor.GetCustomAttribute<SetsRequiredMembersAttribute>();
        Assert.NotNull(setsRequiredMembers);
    }

    [Fact]
    public void FullConstructor_AcceptsAllFieldsInOrder()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Id", typeof(int))
            .AddField("Name", typeof(string), defaultValue: "Default")
            .AddField("Score", typeof(double), isReadOnly: true)
            .WithFullConstructor()
            .Build();

        var constructor = type.GetConstructors().Single(c => c.GetParameters().Length == 3);
        Assert.NotNull(constructor);

        var paramMap = new Dictionary<string, object>
        {
            ["Id"] = 1,
            ["Name"] = "Bob",
            ["Score"] = 99.5
        };
        var args = constructor.GetParameters().Select(p => paramMap[p.Name!]).ToArray();

        var instance = constructor.Invoke(args);
        Assert.NotNull(instance);

        Assert.Equal(1, type.GetField("Id")!.GetValue(instance));
        Assert.Equal("Bob", type.GetField("Name")!.GetValue(instance));
        Assert.Equal(99.5, type.GetField("Score")!.GetValue(instance));
    }

    [Fact]
    public void WithoutConstructors_FallsBackToFullConstructorWhenAllDisabled()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Id", typeof(int))
            .WithParameterlessConstructor()
            .WithMinimalConstructor()
            .WithFullConstructor()
            .WithoutParameterlessConstructor()
            .WithoutMinimalConstructor()
            .WithoutFullConstructor()
            .Build();

        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        Assert.Single(constructors);
        Assert.NotNull(type.GetConstructor(Type.EmptyTypes));
    }

    [Fact]
    public void Constructors_CanCoexistWhenSignaturesDiffer()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Id", typeof(int))
            .AddField("Tag", typeof(string), defaultValue: "DefaultTag")
            .WithParameterlessConstructor()
            .WithMinimalConstructor() // (int id)
            .WithFullConstructor()    // (int id, string tag)
            .Build();

        var parameterlessCtor = type.GetConstructor(Type.EmptyTypes);
        var minimalCtor = type.GetConstructor([typeof(int)]);
        var fullCtor = type.GetConstructors().Single(c => c.GetParameters().Length == 2);

        Assert.NotNull(parameterlessCtor);
        Assert.NotNull(minimalCtor);
        Assert.NotNull(fullCtor);

        var inst1 = parameterlessCtor.Invoke(null);
        Assert.Equal(0, type.GetField("Id")!.GetValue(inst1));
        Assert.Equal("DefaultTag", type.GetField("Tag")!.GetValue(inst1));

        var inst2 = minimalCtor.Invoke([42]);
        Assert.Equal(42, type.GetField("Id")!.GetValue(inst2));
        Assert.Equal("DefaultTag", type.GetField("Tag")!.GetValue(inst2));

        var paramMap = new Dictionary<string, object>
        {
            ["Id"] = 42,
            ["Tag"] = "CustomTag"
        };
        var fullArgs = fullCtor.GetParameters().Select(p => paramMap[p.Name!]).ToArray();

        var inst3 = fullCtor.Invoke(fullArgs);
        Assert.Equal(42, type.GetField("Id")!.GetValue(inst3));
        Assert.Equal("CustomTag", type.GetField("Tag")!.GetValue(inst3));
    }
}
