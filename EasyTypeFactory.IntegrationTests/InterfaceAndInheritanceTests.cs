using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace EasyTypeFactory.IntegrationTests;

public interface IUserEntity
{
    int Id { get; set; }
    string Username { get; }
}

public interface ICaseDifferentInterface
{
    string USERNAME { get; }
}

public interface IChildInterface : IUserEntity
{
    string Email { get; set; }
}

public abstract class BaseEntity
{
    public abstract int Code { get; set; }
    public abstract string Status { get; }
}

public class InterfaceAndInheritanceTests
{
    [Fact]
    public void AddInterface_GeneratesPropertiesWithBackingFields()
    {
        var type = EasyTypeBuilder.Create()
            .AddInterface(typeof(IUserEntity))
            .AddField("Id", typeof(int))
            .AddField("Username", typeof(string), isReadOnly: true)
            .WithFullConstructor()
            .Build();

        Assert.Contains(typeof(IUserEntity), type.GetInterfaces());

        var ctor = type.GetConstructors().Single(c => c.GetParameters().Length == 2);
        var paramMap = new Dictionary<string, object>
        {
            ["Id"] = 123,
            ["Username"] = "john_doe"
        };
        var instance = (IUserEntity)ctor.Invoke(ctor.GetParameters().Select(p => paramMap[p.Name!]).ToArray())!;
        Assert.Equal(123, instance.Id);
        Assert.Equal("john_doe", instance.Username);

        instance.Id = 456;
        Assert.Equal(456, instance.Id);
    }

    [Fact]
    public void CaseInsensitive_GeneratesExplicitInterfaceImplementation_WhenCasingDiffers()
    {
        var type = EasyTypeBuilder.Create()
            .AddInterface(typeof(ICaseDifferentInterface))
            .AddField("Username", typeof(string), isReadOnly: true)
            .CaseInsensitive()
            .WithFullConstructor()
            .Build();

        Assert.Contains(typeof(ICaseDifferentInterface), type.GetInterfaces());

        var instance = (ICaseDifferentInterface)Activator.CreateInstance(type, "case_test")!;
        Assert.Equal("case_test", instance.USERNAME);
    }

    [Fact]
    public void ChildInterface_ScansInheritedInterfacesCorrectly()
    {
        var type = EasyTypeBuilder.Create()
            .AddInterface(typeof(IChildInterface))
            .AddField("Id", typeof(int))
            .AddField("Username", typeof(string), isReadOnly: true)
            .AddField("Email", typeof(string))
            .WithFullConstructor()
            .Build();

        Assert.Contains(typeof(IChildInterface), type.GetInterfaces());
        Assert.Contains(typeof(IUserEntity), type.GetInterfaces());

        var ctor = type.GetConstructors().Single(c => c.GetParameters().Length == 3);
        var paramMap = new Dictionary<string, object>
        {
            ["Id"] = 1,
            ["Username"] = "alice",
            ["Email"] = "alice@example.com"
        };
        var instance = (IChildInterface)ctor.Invoke(ctor.GetParameters().Select(p => paramMap[p.Name!]).ToArray());
        Assert.Equal(1, instance.Id);
        Assert.Equal("alice", instance.Username);
        Assert.Equal("alice@example.com", instance.Email);
    }

    [Fact]
    public void RemoveAndClearInterfaces_RemovesInterfaces()
    {
        var type = EasyTypeBuilder.Create()
            .AddInterface(typeof(IUserEntity))
            .AddInterface(typeof(IChildInterface))
            .RemoveInterface(typeof(IUserEntity))
            .ClearInterfaces()
            .Build();

        Assert.Empty(type.GetInterfaces());
    }

    [Fact]
    public void WithBaseType_ImplementsAbstractMembers()
    {
        var type = EasyTypeBuilder.Create()
            .WithBaseType(typeof(BaseEntity))
            .AddField("Code", typeof(int))
            .AddField("Status", typeof(string), isReadOnly: true)
            .WithFullConstructor()
            .Build();

        Assert.Equal(typeof(BaseEntity), type.BaseType);

        var ctor = type.GetConstructors().Single(c => c.GetParameters().Length == 2);
        var paramMap = new Dictionary<string, object>
        {
            ["Code"] = 999,
            ["Status"] = "Active"
        };
        var instance = (BaseEntity)ctor.Invoke(ctor.GetParameters().Select(p => paramMap[p.Name!]).ToArray())!;
        Assert.Equal(999, instance.Code);
        Assert.Equal("Active", instance.Status);

        instance.Code = 1000;
        Assert.Equal(1000, instance.Code);
    }

    [Fact]
    public void WithoutBaseType_ResetsBaseType()
    {
        var builder = EasyTypeBuilder.Create()
            .WithBaseType(typeof(BaseEntity))
            .WithoutBaseType();

        var type = builder.Build();
        Assert.Equal(typeof(object), type.BaseType);
    }
}
