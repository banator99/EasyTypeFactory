using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace EasyTypeFactory.IntegrationTests;

public enum TestStatus
{
    Inactive = 0,
    Active = 1,
    Pending = 2
}

public class FieldAndPropertyTests
{
    [Fact]
    public void AddField_CreatesPublicField()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Age", typeof(int))
            .AddField("Name", typeof(string))
            .WithParameterlessConstructor()
            .Build();

        var ageField = type.GetField("Age");
        Assert.NotNull(ageField);
        Assert.Equal(typeof(int), ageField.FieldType);
        Assert.True(ageField.IsPublic);
        Assert.False(ageField.IsInitOnly);

        var nameField = type.GetField("Name");
        Assert.NotNull(nameField);
        Assert.Equal(typeof(string), nameField.FieldType);
        Assert.True(nameField.IsPublic);

        var instance = Activator.CreateInstance(type)!;
        ageField.SetValue(instance, 42);
        nameField.SetValue(instance, "Alice");

        Assert.Equal(42, ageField.GetValue(instance));
        Assert.Equal("Alice", nameField.GetValue(instance));
    }

    [Fact]
    public void AddField_ReadOnly_CreatesInitOnlyField()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Id", typeof(Guid), isReadOnly: true)
            .Build();

        var idField = type.GetField("Id");
        Assert.NotNull(idField);
        Assert.True(idField.IsInitOnly);
    }

    [Fact]
    public void AddField_Required_EmitsRequiredMemberAttribute()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Email", typeof(string), isRequired: true)
            .Build();

        var emailField = type.GetField("Email");
        Assert.NotNull(emailField);

        var requiredAttribute = emailField.GetCustomAttribute<RequiredMemberAttribute>();
        Assert.NotNull(requiredAttribute);
    }

    [Fact]
    public void AddField_WithDefaultValues_InitializesCorrectlyInConstructor()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("BoolVal", typeof(bool), defaultValue: true)
            .AddField("ByteVal", typeof(byte), defaultValue: (byte)255)
            .AddField("SByteVal", typeof(sbyte), defaultValue: (sbyte)-10)
            .AddField("ShortVal", typeof(short), defaultValue: (short)-1000)
            .AddField("UShortVal", typeof(ushort), defaultValue: (ushort)5000)
            .AddField("IntVal", typeof(int), defaultValue: 123456)
            .AddField("UIntVal", typeof(uint), defaultValue: 789000u)
            .AddField("LongVal", typeof(long), defaultValue: 9876543210L)
            .AddField("ULongVal", typeof(ulong), defaultValue: 123456789000UL)
            .AddField("FloatVal", typeof(float), defaultValue: 3.14f)
            .AddField("DoubleVal", typeof(double), defaultValue: 2.71828)
            .AddField("CharVal", typeof(char), defaultValue: 'Z')
            .AddField("StringVal", typeof(string), defaultValue: "DefaultText")
            .AddField("EnumVal", typeof(TestStatus), defaultValue: TestStatus.Active)
            .WithParameterlessConstructor()
            .Build();

        var instance = Activator.CreateInstance(type)!;

        Assert.Equal(true, type.GetField("BoolVal")!.GetValue(instance));
        Assert.Equal((byte)255, type.GetField("ByteVal")!.GetValue(instance));
        Assert.Equal((sbyte)-10, type.GetField("SByteVal")!.GetValue(instance));
        Assert.Equal((short)-1000, type.GetField("ShortVal")!.GetValue(instance));
        Assert.Equal((ushort)5000, type.GetField("UShortVal")!.GetValue(instance));
        Assert.Equal(123456, type.GetField("IntVal")!.GetValue(instance));
        Assert.Equal(789000u, type.GetField("UIntVal")!.GetValue(instance));
        Assert.Equal(9876543210L, type.GetField("LongVal")!.GetValue(instance));
        Assert.Equal(123456789000UL, type.GetField("ULongVal")!.GetValue(instance));
        Assert.Equal(3.14f, type.GetField("FloatVal")!.GetValue(instance));
        Assert.Equal(2.71828, type.GetField("DoubleVal")!.GetValue(instance));
        Assert.Equal('Z', type.GetField("CharVal")!.GetValue(instance));
        Assert.Equal("DefaultText", type.GetField("StringVal")!.GetValue(instance));
        Assert.Equal(TestStatus.Active, type.GetField("EnumVal")!.GetValue(instance));
    }

    [Fact]
    public void RemoveField_RemovesSpecifiedField()
    {
        var builder = EasyTypeBuilder.Create()
            .AddField("Field1", typeof(int))
            .AddField("Field2", typeof(string))
            .RemoveField("Field1");

        var type = builder.Build();

        Assert.Null(type.GetField("Field1"));
        Assert.NotNull(type.GetField("Field2"));
    }

    [Fact]
    public void ClearFields_RemovesAllFields()
    {
        var builder = EasyTypeBuilder.Create()
            .AddField("Field1", typeof(int))
            .AddField("Field2", typeof(string))
            .ClearFields();

        var type = builder.Build();

        Assert.Empty(type.GetFields());
    }

    [Fact]
    public void AddField_WithSameName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            EasyTypeBuilder.Create()
                .AddField("Field1", typeof(int), defaultValue: 10)
                .AddField("Field1", typeof(string), defaultValue: "Updated");
        });
    }
}
