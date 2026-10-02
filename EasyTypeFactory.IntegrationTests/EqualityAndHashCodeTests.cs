using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace EasyTypeFactory.IntegrationTests;

public class EqualityAndHashCodeTests
{
    [Fact]
    public void WithEquality_ImplementsIEquatableAndOverridesEqualsAndGetHashCode()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Id", typeof(int))
            .AddField("Name", typeof(string))
            .WithFullConstructor()
            .WithEquality()
            .Build();

        var iequatableInterface = typeof(IEquatable<>).MakeGenericType(type);
        Assert.Contains(iequatableInterface, type.GetInterfaces());

        var ctor = type.GetConstructors().Single(c => c.GetParameters().Length == 2);
        object CreateInst(int id, string name)
        {
            var map = new Dictionary<string, object> { ["Id"] = id, ["Name"] = name };
            return ctor.Invoke(ctor.GetParameters().Select(p => map[p.Name!]).ToArray())!;
        }

        var instance1 = CreateInst(1, "Alpha");
        var instance2 = CreateInst(1, "Alpha");
        var instance3 = CreateInst(2, "Alpha");
        var instance4 = CreateInst(1, "Beta");

        // Object.Equals
        Assert.True(instance1.Equals(instance2));
        Assert.True(instance2.Equals(instance1));
        Assert.False(instance1.Equals(instance3));
        Assert.False(instance1.Equals(instance4));
        Assert.False(instance1.Equals(null));
        Assert.False(instance1.Equals("DifferentType"));

        // GetHashCode
        Assert.Equal(instance1.GetHashCode(), instance2.GetHashCode());

        // Strongly typed Equals via Reflection
        var typedEqualsMethod = type.GetMethod("Equals", [type])!;
        Assert.True((bool)typedEqualsMethod.Invoke(instance1, [instance2])!);
        Assert.False((bool)typedEqualsMethod.Invoke(instance1, [instance3])!);
        Assert.False((bool)typedEqualsMethod.Invoke(instance1, [null])!);
    }

    [Fact]
    public void WithEquality_GeneratesEqualityOperators()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Id", typeof(int))
            .WithFullConstructor()
            .WithEquality(operators: true)
            .Build();

        var opEquality = type.GetMethod("op_Equality", BindingFlags.Public | BindingFlags.Static);
        var opInequality = type.GetMethod("op_Inequality", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(opEquality);
        Assert.NotNull(opInequality);

        var inst1 = Activator.CreateInstance(type, 10);
        var inst2 = Activator.CreateInstance(type, 10);
        var inst3 = Activator.CreateInstance(type, 20);

        Assert.True((bool)opEquality.Invoke(null, [inst1, inst2])!);
        Assert.False((bool)opInequality.Invoke(null, [inst1, inst2])!);

        Assert.False((bool)opEquality.Invoke(null, [inst1, inst3])!);
        Assert.True((bool)opInequality.Invoke(null, [inst1, inst3])!);

        Assert.True((bool)opEquality.Invoke(null, [null, null])!);
        Assert.False((bool)opInequality.Invoke(null, [null, null])!);

        Assert.False((bool)opEquality.Invoke(null, [inst1, null])!);
        Assert.True((bool)opInequality.Invoke(null, [inst1, null])!);

        Assert.False((bool)opEquality.Invoke(null, [null, inst1])!);
        Assert.True((bool)opInequality.Invoke(null, [null, inst1])!);
    }

    [Fact]
    public void WithEquality_WithoutOperators_DoesNotEmitOperators()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Id", typeof(int))
            .WithEquality(operators: false)
            .Build();

        var opEquality = type.GetMethod("op_Equality", BindingFlags.Public | BindingFlags.Static);
        var opInequality = type.GetMethod("op_Inequality", BindingFlags.Public | BindingFlags.Static);

        Assert.Null(opEquality);
        Assert.Null(opInequality);
    }

    [Fact]
    public void WithoutEquality_RemovesEqualityMembers()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Id", typeof(int))
            .WithEquality()
            .WithoutEquality()
            .Build();

        var iequatableInterface = typeof(IEquatable<>).MakeGenericType(type);
        Assert.DoesNotContain(iequatableInterface, type.GetInterfaces());

        var opEquality = type.GetMethod("op_Equality", BindingFlags.Public | BindingFlags.Static);
        Assert.Null(opEquality);
    }
}
