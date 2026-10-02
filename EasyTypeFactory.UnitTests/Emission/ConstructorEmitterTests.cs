using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Emission;
using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Emission;

public class ConstructorEmitterTests
{
    public enum SampleEnum { OptionA = 1, OptionB = 2 }

    [Fact]
    public void Emit_ParameterlessConstructor_InitializesDefaultValues()
    {
        var assemblyName = new AssemblyName("TestAssembly_" + Guid.NewGuid().ToString("N"));
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        var typeBuilder = moduleBuilder.DefineType("TestType_" + Guid.NewGuid().ToString("N"), TypeAttributes.Public | TypeAttributes.Class);

        var fBool = new ModelField { Name = "_bool", OriginalName = "Bool", Type = typeof(bool), DefaultValue = true };
        var fByte = new ModelField { Name = "_byte", OriginalName = "Byte", Type = typeof(byte), DefaultValue = (byte)5 };
        var fSByte = new ModelField { Name = "_sbyte", OriginalName = "SByte", Type = typeof(sbyte), DefaultValue = (sbyte)-5 };
        var fShort = new ModelField { Name = "_short", OriginalName = "Short", Type = typeof(short), DefaultValue = (short)100 };
        var fUShort = new ModelField { Name = "_ushort", OriginalName = "UShort", Type = typeof(ushort), DefaultValue = (ushort)200 };
        var fInt = new ModelField { Name = "_int", OriginalName = "Int", Type = typeof(int), DefaultValue = 300 };
        var fUInt = new ModelField { Name = "_uint", OriginalName = "UInt", Type = typeof(uint), DefaultValue = 400u };
        var fLong = new ModelField { Name = "_long", OriginalName = "Long", Type = typeof(long), DefaultValue = 500L };
        var fULong = new ModelField { Name = "_ulong", OriginalName = "ULong", Type = typeof(ulong), DefaultValue = 600UL };
        var fFloat = new ModelField { Name = "_float", OriginalName = "Float", Type = typeof(float), DefaultValue = 1.5f };
        var fDouble = new ModelField { Name = "_double", OriginalName = "Double", Type = typeof(double), DefaultValue = 2.5 };
        var fChar = new ModelField { Name = "_char", OriginalName = "Char", Type = typeof(char), DefaultValue = 'Z' };
        var fStr = new ModelField { Name = "_str", OriginalName = "Str", Type = typeof(string), DefaultValue = "Hello" };
        var fEnum = new ModelField { Name = "_enum", OriginalName = "Enum", Type = typeof(SampleEnum), DefaultValue = SampleEnum.OptionB };
        var fNullStr = new ModelField { Name = "_nullStr", OriginalName = "NullStr", Type = typeof(string), DefaultValue = null };
        var fZeroInt = new ModelField { Name = "_zeroInt", OriginalName = "ZeroInt", Type = typeof(int), DefaultValue = 0 };

        var fields = new Dictionary<ModelField, FieldBuilder>();
        foreach (var f in new[] { fBool, fByte, fSByte, fShort, fUShort, fInt, fUInt, fLong, fULong, fFloat, fDouble, fChar, fStr, fEnum, fNullStr, fZeroInt })
        {
            var fb = typeBuilder.DefineField(f.Name, f.Type, FieldAttributes.Public);
            fields.Add(f, fb);
        }

        var constructor = new ModelConstructor
        {
            Fields = [],
            SetsRequiredMembers = true
        };

        var emitter = new ConstructorEmitter(typeBuilder, constructor, fields);
        emitter.Emit();

        var type = typeBuilder.CreateType();
        var ctorInfo = type.GetConstructor(Type.EmptyTypes)!;
        Assert.NotNull(ctorInfo);
        Assert.NotNull(ctorInfo.GetCustomAttribute<SetsRequiredMembersAttribute>());

        var instance = ctorInfo.Invoke(null)!;
        Assert.Equal(true, type.GetField("_bool")!.GetValue(instance));
        Assert.Equal((byte)5, type.GetField("_byte")!.GetValue(instance));
        Assert.Equal((sbyte)-5, type.GetField("_sbyte")!.GetValue(instance));
        Assert.Equal((short)100, type.GetField("_short")!.GetValue(instance));
        Assert.Equal((ushort)200, type.GetField("_ushort")!.GetValue(instance));
        Assert.Equal(300, type.GetField("_int")!.GetValue(instance));
        Assert.Equal(400u, type.GetField("_uint")!.GetValue(instance));
        Assert.Equal(500L, type.GetField("_long")!.GetValue(instance));
        Assert.Equal(600UL, type.GetField("_ulong")!.GetValue(instance));
        Assert.Equal(1.5f, type.GetField("_float")!.GetValue(instance));
        Assert.Equal(2.5, type.GetField("_double")!.GetValue(instance));
        Assert.Equal('Z', type.GetField("_char")!.GetValue(instance));
        Assert.Equal("Hello", type.GetField("_str")!.GetValue(instance));
        Assert.Equal(SampleEnum.OptionB, type.GetField("_enum")!.GetValue(instance));
        Assert.Null(type.GetField("_nullStr")!.GetValue(instance));
        Assert.Equal(0, type.GetField("_zeroInt")!.GetValue(instance));
    }

    [Fact]
    public void Emit_ParameterizedConstructor_AssignsArgumentsToFields()
    {
        var assemblyName = new AssemblyName("TestAssembly_" + Guid.NewGuid().ToString("N"));
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        var typeBuilder = moduleBuilder.DefineType("TestType_" + Guid.NewGuid().ToString("N"), TypeAttributes.Public | TypeAttributes.Class);

        var fId = new ModelField { Name = "_id", OriginalName = "Id", Type = typeof(int), ReadOnly = true };
        var fName = new ModelField { Name = "_name", OriginalName = "Name", Type = typeof(string), ReadOnly = false };

        var fbId = typeBuilder.DefineField("_id", typeof(int), FieldAttributes.Public | FieldAttributes.InitOnly);
        var fbName = typeBuilder.DefineField("_name", typeof(string), FieldAttributes.Public);

        var fields = new Dictionary<ModelField, FieldBuilder>
        {
            [fId] = fbId,
            [fName] = fbName
        };

        var constructor = new ModelConstructor
        {
            Fields = [fId, fName],
            SetsRequiredMembers = false
        };

        var emitter = new ConstructorEmitter(typeBuilder, constructor, fields);
        emitter.Emit();

        var type = typeBuilder.CreateType();
        var ctorInfo = type.GetConstructor([typeof(int), typeof(string)])!;
        Assert.NotNull(ctorInfo);

        var instance = ctorInfo.Invoke([123, "Alice"])!;
        Assert.Equal(123, type.GetField("_id")!.GetValue(instance));
        Assert.Equal("Alice", type.GetField("_name")!.GetValue(instance));
    }

    [Fact]
    public void Emit_WithUnsupportedDefaultValueType_ThrowsNotSupportedException()
    {
        var assemblyName = new AssemblyName("TestAssembly_" + Guid.NewGuid().ToString("N"));
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        var typeBuilder = moduleBuilder.DefineType("TestType_" + Guid.NewGuid().ToString("N"), TypeAttributes.Public | TypeAttributes.Class);

        var fCustom = new ModelField { Name = "_custom", OriginalName = "Custom", Type = typeof(DateTime), DefaultValue = DateTime.Now };
        var fbCustom = typeBuilder.DefineField("_custom", typeof(DateTime), FieldAttributes.Public);

        var fields = new Dictionary<ModelField, FieldBuilder>
        {
            [fCustom] = fbCustom
        };

        var constructor = new ModelConstructor
        {
            Fields = [],
            SetsRequiredMembers = false
        };

        var emitter = new ConstructorEmitter(typeBuilder, constructor, fields);
        Assert.Throws<NotSupportedException>(() => emitter.Emit());
    }
}
