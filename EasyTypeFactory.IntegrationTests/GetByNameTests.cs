using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace EasyTypeFactory.IntegrationTests;

public class GetByNameTests
{
    [Fact]
    public void GetByName_RetrievesFieldValues()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Id", typeof(int), defaultValue: 42)
            .AddField("Name", typeof(string), defaultValue: "TestObject")
            .WithParameterlessConstructor()
            .WithGetByName()
            .Build();

        var instance = Activator.CreateInstance(type)!;
        var getByName = type.GetMethod("GetByName", [typeof(string)])!;

        Assert.Equal(42, getByName.Invoke(instance, ["Id"]));
        Assert.Equal("TestObject", getByName.Invoke(instance, ["Name"]));

        var ex = Assert.Throws<TargetInvocationException>(() => getByName.Invoke(instance, ["NonExistent"]));
        Assert.IsType<KeyNotFoundException>(ex.InnerException);
    }

    [Fact]
    public void GetByName_CustomMethodName_UsesSpecifiedName()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Code", typeof(string), defaultValue: "ABC")
            .WithParameterlessConstructor()
            .WithGetByName("FetchValue")
            .Build();

        var instance = Activator.CreateInstance(type)!;
        var fetchValue = type.GetMethod("FetchValue", [typeof(string)])!;
        Assert.NotNull(fetchValue);
        Assert.Equal("ABC", fetchValue.Invoke(instance, ["Code"]));
    }

    [Fact]
    public void WithoutGetByName_DoesNotEmitGetByName()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Code", typeof(string))
            .WithGetByName()
            .WithoutGetByName()
            .Build();

        Assert.Null(type.GetMethod("GetByName", [typeof(string)]));
    }

    [Fact]
    public void GenericGetByName_DirectTypeMatch_ReturnsUnboxedValue()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Count", typeof(int), defaultValue: 99)
            .AddField("Title", typeof(string), defaultValue: "Book")
            .WithParameterlessConstructor()
            .WithGenericGetByName()
            .Build();

        var instance = Activator.CreateInstance(type)!;
        var genericMethod = type.GetMethod("GetByName", 1, [typeof(string)])!;

        var getIntMethod = genericMethod.MakeGenericMethod(typeof(int));
        Assert.Equal(99, getIntMethod.Invoke(instance, ["Count"]));

        var getStringMethod = genericMethod.MakeGenericMethod(typeof(string));
        Assert.Equal("Book", getStringMethod.Invoke(instance, ["Title"]));
    }

    [Fact]
    public void GenericGetByName_AssignableType_ReturnsCastValue()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Title", typeof(string), defaultValue: "AssignableTest")
            .WithParameterlessConstructor()
            .WithGenericGetByName()
            .Build();

        var instance = Activator.CreateInstance(type)!;
        var genericMethod = type.GetMethod("GetByName", 1, [typeof(string)])!;

        // string is assignable to object and IComparable
        var getObjectMethod = genericMethod.MakeGenericMethod(typeof(object));
        Assert.Equal("AssignableTest", getObjectMethod.Invoke(instance, ["Title"]));

        var getComparableMethod = genericMethod.MakeGenericMethod(typeof(IComparable));
        Assert.Equal("AssignableTest", getComparableMethod.Invoke(instance, ["Title"]));
    }

    [Fact]
    public void GenericGetByName_IncompatibleType_ThrowsInvalidOperationException()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Count", typeof(int), defaultValue: 99)
            .WithParameterlessConstructor()
            .WithGenericGetByName()
            .Build();

        var instance = Activator.CreateInstance(type)!;
        var genericMethod = type.GetMethod("GetByName", 1, [typeof(string)])!;
        var getStringMethod = genericMethod.MakeGenericMethod(typeof(string));

        var ex = Assert.Throws<TargetInvocationException>(() => getStringMethod.Invoke(instance, ["Count"]));
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    [Fact]
    public void GenericGetByName_MissingField_ThrowsKeyNotFoundException()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Count", typeof(int), defaultValue: 99)
            .WithParameterlessConstructor()
            .WithGenericGetByName()
            .Build();

        var instance = Activator.CreateInstance(type)!;
        var genericMethod = type.GetMethod("GetByName", 1, [typeof(string)])!;
        var getIntMethod = genericMethod.MakeGenericMethod(typeof(int));

        var ex = Assert.Throws<TargetInvocationException>(() => getIntMethod.Invoke(instance, ["Unknown"]));
        Assert.IsType<KeyNotFoundException>(ex.InnerException);
    }

    [Fact]
    public void AddGetTypeByName_GeneratesTypedGetters()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Number", typeof(int), defaultValue: 55)
            .AddField("Description", typeof(string), defaultValue: "Hello")
            .WithParameterlessConstructor()
            .AddGetTypeByName(typeof(int))
            .AddGetTypeByName("RetrieveString", typeof(string))
            .Build();

        var instance = Activator.CreateInstance(type)!;

        var getIntMethod = type.GetMethod("GetInt32ByName", [typeof(string)])!;
        Assert.NotNull(getIntMethod);
        Assert.Equal(55, getIntMethod.Invoke(instance, ["Number"]));

        var exType = Assert.Throws<TargetInvocationException>(() => getIntMethod.Invoke(instance, ["Description"]));
        Assert.IsType<InvalidOperationException>(exType.InnerException);

        var exMissing = Assert.Throws<TargetInvocationException>(() => getIntMethod.Invoke(instance, ["Missing"]));
        Assert.IsType<KeyNotFoundException>(exMissing.InnerException);

        var retrieveStringMethod = type.GetMethod("RetrieveString", [typeof(string)])!;
        Assert.NotNull(retrieveStringMethod);
        Assert.Equal("Hello", retrieveStringMethod.Invoke(instance, ["Description"]));
    }

    [Fact]
    public void DoAddAllGetTypeByNameMethods_GeneratesGettersForAllFieldTypes()
    {
        var builder = EasyTypeBuilder.Create()
            .AddField("Id", typeof(int), defaultValue: 1)
            .AddField("Name", typeof(string), defaultValue: "A")
            .AddField("Flag", typeof(bool), defaultValue: true)
            .WithParameterlessConstructor()
            .DoAddAllGetTypeByNameMethods();

        var type = builder.Build();

        Assert.NotNull(type.GetMethod("GetInt32ByName", [typeof(string)]));
        Assert.NotNull(type.GetMethod("GetStringByName", [typeof(string)]));
        Assert.NotNull(type.GetMethod("GetBooleanByName", [typeof(string)]));

        var builderDisabled = builder.DoNotAddAllGetTypeByNameMethods();
        var typeDisabled = builderDisabled.Build();
        Assert.Null(typeDisabled.GetMethod("GetInt32ByName", [typeof(string)]));
    }

    [Fact]
    public void RemoveAndClearGetTypeByName_RemovesMethods()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Id", typeof(int))
            .AddGetTypeByName(typeof(int))
            .AddGetTypeByName(typeof(string))
            .RemoveGetTypeByName(typeof(int))
            .ClearGetTypeByNames()
            .Build();

        Assert.Null(type.GetMethod("GetInt32ByName", [typeof(string)]));
        Assert.Null(type.GetMethod("GetStringByName", [typeof(string)]));
    }

    [Fact]
    public void GetTypeByName_WithManyUnsupportedFields_EmitsLongBranchesCorrectly()
    {
        var builder = EasyTypeBuilder.Create()
            .AddField("TargetString", typeof(string), defaultValue: "Target");

        for (var i = 0; i < 25; i++)
        {
            builder = builder.AddField($"IntField{i}", typeof(int), defaultValue: i);
        }

        builder = builder
            .WithParameterlessConstructor()
            .AddGetTypeByName(typeof(string));

        var type = builder.Build();
        var instance = Activator.CreateInstance(type)!;
        var getString = type.GetMethod("GetStringByName", [typeof(string)])!;

        Assert.Equal("Target", getString.Invoke(instance, ["TargetString"]));

        var exUnsupported = Assert.Throws<TargetInvocationException>(() => getString.Invoke(instance, ["IntField10"]));
        Assert.IsType<InvalidOperationException>(exUnsupported.InnerException);
    }

    [Fact]
    public void CaseInsensitive_GetByName_MatchesCaseInsensitively()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("UserProfile", typeof(string), defaultValue: "Admin")
            .WithParameterlessConstructor()
            .WithGetByName()
            .WithGenericGetByName()
            .AddGetTypeByName(typeof(string))
            .CaseInsensitive()
            .Build();

        var instance = Activator.CreateInstance(type)!;

        var getByName = type.GetMethods().Single(m => m.Name == "GetByName" && !m.IsGenericMethod);
        Assert.Equal("Admin", getByName.Invoke(instance, ["userprofile"]));
        Assert.Equal("Admin", getByName.Invoke(instance, ["USERPROFILE"]));

        var genericGet = type.GetMethods().Single(m => m.Name == "GetByName" && m.IsGenericMethod)
            .MakeGenericMethod(typeof(string));
        Assert.Equal("Admin", genericGet.Invoke(instance, ["uSeRpRoFiLe"]));

        var getString = type.GetMethod("GetStringByName", [typeof(string)])!;
        Assert.Equal("Admin", getString.Invoke(instance, ["userPROFILE"]));
    }

    [Fact]
    public void CaseSensitive_GetByName_RequiresExactCasing()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("UserProfile", typeof(string), defaultValue: "Admin")
            .WithParameterlessConstructor()
            .WithGetByName()
            .CaseInsensitive()
            .CaseSensitive()
            .Build();

        var instance = Activator.CreateInstance(type)!;
        var getByName = type.GetMethod("GetByName", [typeof(string)])!;

        Assert.Equal("Admin", getByName.Invoke(instance, ["UserProfile"]));

        var ex = Assert.Throws<TargetInvocationException>(() => getByName.Invoke(instance, ["userprofile"]));
        Assert.IsType<KeyNotFoundException>(ex.InnerException);
    }
}
