using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Definition;
using Xunit;

namespace EasyTypeFactory.UnitTests.Definition;

public class CustomMethodDefinitionTests
{
    [Fact]
    public void Constructor_WithDefaultValues_InitializesProperties()
    {
        var def = new CustomMethodDefinition("TestMethod");
        Assert.Equal("TestMethod", def.Name);
        Assert.Equal(MethodAttributes.Public, def.Attributes);
        Assert.Null(def.ReturnType);
        Assert.NotNull(def.ParameterTypes);
        Assert.Empty(def.ParameterTypes);
        Assert.NotNull(def.Modifiers);
        Assert.Empty(def.Modifiers);
        Assert.Null(def.Emitter);
    }

    [Fact]
    public void Equals_And_GetHashCode_WorkCorrectly()
    {
        Action<ILGenerator, MethodBuilderContext> emitter = (il, ctx) => { };
        Action<MethodBuilder, MethodBuilderContext> modifier = (mb, ctx) => { };

        var def1 = new CustomMethodDefinition(
            "MethodA",
            MethodAttributes.Public | MethodAttributes.Virtual,
            typeof(int),
            [typeof(string), typeof(int)],
            ImmutableList.Create(modifier),
            emitter);

        var def2 = new CustomMethodDefinition(
            "MethodA",
            MethodAttributes.Public | MethodAttributes.Virtual,
            typeof(int),
            [typeof(string), typeof(int)],
            ImmutableList.Create(modifier),
            emitter);

        var defDiffName = def1 with { Name = "MethodB" };
        var defDiffAttr = def1 with { Attributes = MethodAttributes.Private };
        var defDiffRet = def1 with { ReturnType = typeof(string) };
        var defDiffParam = def1 with { ParameterTypes = [typeof(string)] };
        var defNullParam = def1 with { ParameterTypes = null };
        var defDiffMod = def1 with { Modifiers = ImmutableList<Action<MethodBuilder, MethodBuilderContext>>.Empty };
        var defDiffEmit = def1 with { Emitter = (il, ctx) => il.Emit(OpCodes.Ret) };

        Assert.True(def1.Equals(def2));
        Assert.True(def1.Equals((object)def2));
        Assert.True(def1.Equals(def1));
        Assert.False(def1.Equals(null));
        Assert.False(def1.Equals((object?)null));
        Assert.False(def1.Equals("other"));

        Assert.False(def1.Equals(defDiffName));
        Assert.False(def1.Equals(defDiffAttr));
        Assert.False(def1.Equals(defDiffRet));
        Assert.False(def1.Equals(defDiffParam));
        Assert.False(def1.Equals(defNullParam));
        Assert.False(def1.Equals(defDiffMod));
        Assert.False(def1.Equals(defDiffEmit));

        var nullParam1 = new CustomMethodDefinition("M", MethodAttributes.Public, null, null, ImmutableList<Action<MethodBuilder, MethodBuilderContext>>.Empty, null);
        var nullParam2 = new CustomMethodDefinition("M", MethodAttributes.Public, null, null, ImmutableList<Action<MethodBuilder, MethodBuilderContext>>.Empty, null);
        Assert.True(nullParam1.Equals(nullParam2));

        Assert.Equal(def1.GetHashCode(), def2.GetHashCode());
    }
}
