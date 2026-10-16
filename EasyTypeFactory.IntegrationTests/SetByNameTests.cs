using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace EasyTypeFactory.IntegrationTests;

public class SetByNameTests
{
    [Fact]
    public void SetByName_SetsFieldValuesCorrectly()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Id", typeof(int))
            .AddField("Name", typeof(string))
            .WithParameterlessConstructor()
            .WithSetByName()
            .Build();

        var instance = Activator.CreateInstance(type)!;
        var setByName = type.GetMethod("SetByName", [typeof(string), typeof(object)])!;

        setByName.Invoke(instance, ["Id", 123]);
        setByName.Invoke(instance, ["Name", "UpdatedName"]);

        Assert.Equal(123, type.GetField("Id")!.GetValue(instance));
        Assert.Equal("UpdatedName", type.GetField("Name")!.GetValue(instance));

        // Null value for reference type
        setByName.Invoke(instance, ["Name", null]);
        Assert.Null(type.GetField("Name")!.GetValue(instance));
    }

    [Fact]
    public void SetByName_ReadOnlyField_ThrowsInvalidOperationException()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Id", typeof(int), isReadOnly: true)
            .WithSetByName()
            .Build();

        var instance = Activator.CreateInstance(type, 100)!;
        var setByName = type.GetMethod("SetByName", [typeof(string), typeof(object)])!;

        var ex = Assert.Throws<TargetInvocationException>(() => setByName.Invoke(instance, ["Id", 999]));
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    [Fact]
    public void SetByName_IncompatibleType_ThrowsInvalidOperationException()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Id", typeof(int))
            .WithParameterlessConstructor()
            .WithSetByName()
            .Build();

        var instance = Activator.CreateInstance(type)!;
        var setByName = type.GetMethod("SetByName", [typeof(string), typeof(object)])!;

        var ex = Assert.Throws<TargetInvocationException>(() => setByName.Invoke(instance, ["Id", "NotAnInt"]));
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    [Fact]
    public void SetByName_MissingField_ThrowsKeyNotFoundException()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Id", typeof(int))
            .WithParameterlessConstructor()
            .WithSetByName()
            .Build();

        var instance = Activator.CreateInstance(type)!;
        var setByName = type.GetMethod("SetByName", [typeof(string), typeof(object)])!;

        var ex = Assert.Throws<TargetInvocationException>(() => setByName.Invoke(instance, ["Unknown", 10]));
        Assert.IsType<KeyNotFoundException>(ex.InnerException);
    }

    [Fact]
    public void SetByName_CustomMethodName_UsesSpecifiedName()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Code", typeof(string))
            .WithParameterlessConstructor()
            .WithSetByName("UpdateField")
            .Build();

        var instance = Activator.CreateInstance(type)!;
        var updateField = type.GetMethod("UpdateField", [typeof(string), typeof(object)])!;

        Assert.NotNull(updateField);
        updateField.Invoke(instance, ["Code", "NewCode"]);
        Assert.Equal("NewCode", type.GetField("Code")!.GetValue(instance));
    }

    [Fact]
    public void WithoutSetByName_RemovesSetByNameMethod()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Code", typeof(string))
            .WithSetByName()
            .WithoutSetByName()
            .Build();

        Assert.Null(type.GetMethod("SetByName", [typeof(string), typeof(object)]));
    }

    [Fact]
    public void GenericSetByName_SetsFieldValues()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Id", typeof(int))
            .AddField("Description", typeof(string))
            .WithParameterlessConstructor()
            .WithGenericSetByName()
            .Build();

        var instance = Activator.CreateInstance(type)!;
        var genericSet = type.GetMethod("SetByName", 1, [typeof(string), Type.MakeGenericMethodParameter(0)])!;

        var setIntMethod = genericSet.MakeGenericMethod(typeof(int));
        setIntMethod.Invoke(instance, ["Id", 456]);

        var setStringMethod = genericSet.MakeGenericMethod(typeof(string));
        setStringMethod.Invoke(instance, ["Description", "GenericDesc"]);

        Assert.Equal(456, type.GetField("Id")!.GetValue(instance));
        Assert.Equal("GenericDesc", type.GetField("Description")!.GetValue(instance));

        var ex = Assert.Throws<TargetInvocationException>(() => setIntMethod.Invoke(instance, ["Missing", 1]));
        Assert.IsType<KeyNotFoundException>(ex.InnerException);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void GenericSetByName_ReadOnlyFields_ThrowWithoutChangingValues(bool caseInsensitive, bool useObject)
    {
        var builder = EasyTypeBuilder.Create()
            .AddField("Id", typeof(int), isReadOnly: true)
            .AddField("Tag", typeof(string), isReadOnly: true)
            .AddField("Count", typeof(int))
            .WithFullConstructor()
            .WithGenericSetByName();
        if (caseInsensitive)
        {
            builder = builder.CaseInsensitive();
        }

        var type = builder.Build();
        var ctor = type.GetConstructors().Single(c => c.GetParameters().Length == 3);
        var values = new Dictionary<string, object> { ["Id"] = 100, ["Tag"] = "Initial", ["Count"] = 0 };
        var instance = ctor.Invoke(ctor.GetParameters().Select(p => values[p.Name!]).ToArray());
        var genericSet = type.GetMethod("SetByName", 1, [typeof(string), Type.MakeGenericMethodParameter(0)])!;

        foreach (var name in new[] { "Id", "Tag" })
        {
            var field = type.GetField(name)!;
            var setMethod = genericSet.MakeGenericMethod(useObject ? typeof(object) : field.FieldType);
            var lookupName = caseInsensitive ? name.ToLowerInvariant() : name;
            object replacement = name == "Id" ? 999 : "Updated";

            var ex = Assert.Throws<TargetInvocationException>(() => setMethod.Invoke(instance, [lookupName, replacement]));
            var inner = Assert.IsType<InvalidOperationException>(ex.InnerException);
            Assert.Equal($"Cannot set field '{name}' because it's read-only.", inner.Message);
            Assert.Equal(values[name], field.GetValue(instance));
        }

        var setInt = genericSet.MakeGenericMethod(typeof(int));
        setInt.Invoke(instance, [caseInsensitive ? "count" : "Count", 42]);
        Assert.Equal(42, type.GetField("Count")!.GetValue(instance));

        var missing = Assert.Throws<TargetInvocationException>(() => setInt.Invoke(instance, ["Missing", 1]));
        Assert.IsType<KeyNotFoundException>(missing.InnerException);
    }

    [Fact]
    public void AddSetTypeByName_GeneratesTypedSetters()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Count", typeof(int))
            .AddField("Label", typeof(string))
            .AddField("ReadOnlyTag", typeof(string), isReadOnly: true)
            .WithFullConstructor()
            .AddSetTypeByName(typeof(int))
            .AddSetTypeByName("AssignString", typeof(string))
            .Build();

        var ctor = type.GetConstructors().Single(c => c.GetParameters().Length == 3);
        var paramMap = new Dictionary<string, object>
        {
            ["Count"] = 0,
            ["Label"] = "Initial",
            ["ReadOnlyTag"] = "InitialTag"
        };
        var instance = ctor.Invoke(ctor.GetParameters().Select(p => paramMap[p.Name!]).ToArray())!;

        var setIntMethod = type.GetMethod("SetInt32ByName", [typeof(string), typeof(int)])!;
        Assert.NotNull(setIntMethod);
        setIntMethod.Invoke(instance, ["Count", 777]);
        Assert.Equal(777, type.GetField("Count")!.GetValue(instance));

        var assignStringMethod = type.GetMethod("AssignString", [typeof(string), typeof(string)])!;
        Assert.NotNull(assignStringMethod);
        assignStringMethod.Invoke(instance, ["Label", "CustomLabel"]);
        Assert.Equal("CustomLabel", type.GetField("Label")!.GetValue(instance));

        // Read-only check in typed setter
        var exReadOnly = Assert.Throws<TargetInvocationException>(() => assignStringMethod.Invoke(instance, ["ReadOnlyTag", "Val"]));
        Assert.IsType<InvalidOperationException>(exReadOnly.InnerException);

        // Type mismatch in typed setter
        var exMismatch = Assert.Throws<TargetInvocationException>(() => setIntMethod.Invoke(instance, ["Label", 123]));
        Assert.IsType<InvalidOperationException>(exMismatch.InnerException);

        // Missing field
        var exMissing = Assert.Throws<TargetInvocationException>(() => setIntMethod.Invoke(instance, ["NonExistent", 123]));
        Assert.IsType<KeyNotFoundException>(exMissing.InnerException);
    }

    [Fact]
    public void DoAddAllSetTypeByNameMethods_GeneratesSettersForAllFieldTypes()
    {
        var builder = EasyTypeBuilder.Create()
            .AddField("Id", typeof(int))
            .AddField("Name", typeof(string))
            .AddField("Flag", typeof(bool))
            .WithParameterlessConstructor()
            .DoAddAllSetTypeByNameMethods();

        var type = builder.Build();

        Assert.NotNull(type.GetMethod("SetInt32ByName", [typeof(string), typeof(int)]));
        Assert.NotNull(type.GetMethod("SetStringByName", [typeof(string), typeof(string)]));
        Assert.NotNull(type.GetMethod("SetBooleanByName", [typeof(string), typeof(bool)]));

        var builderDisabled = builder.DoNotAddAllSetTypeByNameMethods();
        var typeDisabled = builderDisabled.Build();
        Assert.Null(typeDisabled.GetMethod("SetInt32ByName", [typeof(string), typeof(int)]));
    }

    [Fact]
    public void RemoveAndClearSetTypeByName_RemovesMethods()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("Id", typeof(int))
            .AddSetTypeByName(typeof(int))
            .AddSetTypeByName(typeof(string))
            .RemoveSetTypeByName(typeof(int))
            .ClearSetTypeByNames()
            .Build();

        Assert.Null(type.GetMethod("SetInt32ByName", [typeof(string), typeof(int)]));
        Assert.Null(type.GetMethod("SetStringByName", [typeof(string), typeof(string)]));
    }

    [Fact]
    public void CaseInsensitive_SetByName_MatchesCaseInsensitively()
    {
        var type = EasyTypeBuilder.Create()
            .AddField("ScoreValue", typeof(int))
            .WithParameterlessConstructor()
            .WithSetByName()
            .WithGenericSetByName()
            .AddSetTypeByName(typeof(int))
            .CaseInsensitive()
            .Build();

        var instance = Activator.CreateInstance(type)!;

        var setByName = type.GetMethod("SetByName", [typeof(string), typeof(object)])!;
        setByName.Invoke(instance, ["scorevalue", 100]);
        Assert.Equal(100, type.GetField("ScoreValue")!.GetValue(instance));

        var genericSet = type.GetMethod("SetByName", 1, [typeof(string), Type.MakeGenericMethodParameter(0)])!
            .MakeGenericMethod(typeof(int));
        genericSet.Invoke(instance, ["SCOREVALUE", 200]);
        Assert.Equal(200, type.GetField("ScoreValue")!.GetValue(instance));

        var setInt = type.GetMethod("SetInt32ByName", [typeof(string), typeof(int)])!;
        setInt.Invoke(instance, ["scoreVALUE", 300]);
        Assert.Equal(300, type.GetField("ScoreValue")!.GetValue(instance));
    }
}
