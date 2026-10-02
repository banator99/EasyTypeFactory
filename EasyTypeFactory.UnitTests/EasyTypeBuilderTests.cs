using System;
using System.Reflection.Emit;
using EasyTypeFactory;
using EasyTypeFactory.Utils;
using Xunit;

namespace EasyTypeFactory.UnitTests;

public class EasyTypeBuilderTests
{
    [Fact]
    public void Create_And_Empty_Default_Instances()
    {
        var empty = EasyTypeBuilder.Empty;
        Assert.NotNull(empty);

        var def = EasyTypeBuilder.Default;
        Assert.NotNull(def);

        var createdNull = EasyTypeBuilder.Create(null);
        Assert.NotNull(createdNull);

        var createdName = EasyTypeBuilder.Create("MyType", discriminate: false);
        Assert.NotNull(createdName);
    }

    [Fact]
    public void Builder_FluentMethods_ReturnNewInstances()
    {
        var b0 = EasyTypeBuilder.Create();
        var bDefaults = b0.Defaults();
        Assert.NotSame(b0, bDefaults);

        var bModule = b0.WithModuleBuilder(Shared.ModuleBuilder);
        Assert.NotSame(b0, bModule);

        var bNoModule = bModule.WithoutModuleBuilder();
        Assert.NotSame(bModule, bNoModule);

        var bName = b0.WithName("TestName", discriminate: false);
        Assert.NotSame(b0, bName);

        var bNoName = bName.WithoutName();
        Assert.NotSame(bName, bNoName);

        var bBase = b0.WithBaseType(typeof(object));
        Assert.NotSame(b0, bBase);

        var bNoBase = bBase.WithoutBaseType();
        Assert.NotSame(bBase, bNoBase);

        var bField = b0.AddField("Field1", typeof(string), isReadOnly: true, isRequired: true, defaultValue: "val");
        Assert.NotSame(b0, bField);

        var bRemoveField = bField.RemoveField("Field1");
        Assert.NotSame(bField, bRemoveField);

        var bClearFields = bField.ClearFields();
        Assert.NotSame(bField, bClearFields);

        var bInterface = b0.AddInterface(typeof(IDisposable));
        Assert.NotSame(b0, bInterface);

        var bRemoveInterface = bInterface.RemoveInterface(typeof(IDisposable));
        Assert.NotSame(bInterface, bRemoveInterface);

        var bClearInterfaces = bInterface.ClearInterfaces();
        Assert.NotSame(bInterface, bClearInterfaces);

        var bGetByName = b0.WithGetByName("GetByNameCustom");
        Assert.NotSame(b0, bGetByName);

        var bNoGetByName = bGetByName.WithoutGetByName();
        Assert.NotSame(bGetByName, bNoGetByName);

        var bGenGet = b0.WithGenericGetByName("GetTByNameCustom");
        Assert.NotSame(b0, bGenGet);

        var bNoGenGet = bGenGet.WithoutGenericGetByName();
        Assert.NotSame(bGenGet, bNoGenGet);

        var bGetType1 = b0.AddGetTypeByName(typeof(int));
        var bGetType2 = b0.AddGetTypeByName("GetIntVal", typeof(int));
        Assert.NotSame(b0, bGetType1);
        Assert.NotSame(b0, bGetType2);

        var bRemoveGetType = bGetType1.RemoveGetTypeByName(typeof(int));
        Assert.NotSame(bGetType1, bRemoveGetType);

        var bClearGetType = bGetType1.ClearGetTypeByNames();
        Assert.NotSame(bGetType1, bClearGetType);

        var bAddAllGet = b0.DoAddAllGetTypeByNameMethods();
        var bNotAddAllGet = bAddAllGet.DoNotAddAllGetTypeByNameMethods();
        Assert.NotSame(b0, bAddAllGet);
        Assert.NotSame(bAddAllGet, bNotAddAllGet);

        var bSetByName = b0.WithSetByName("SetByNameCustom");
        var bNoSetByName = bSetByName.WithoutSetByName();
        Assert.NotSame(b0, bSetByName);
        Assert.NotSame(bSetByName, bNoSetByName);

        var bGenSet = b0.WithGenericSetByName("SetTByNameCustom");
        var bNoGenSet = bGenSet.WithoutGenericSetByName();
        Assert.NotSame(b0, bGenSet);
        Assert.NotSame(bGenSet, bNoGenSet);

        var bSetType1 = b0.AddSetTypeByName(typeof(int));
        var bSetType2 = b0.AddSetTypeByName("SetIntVal", typeof(int));
        Assert.NotSame(b0, bSetType1);
        Assert.NotSame(b0, bSetType2);

        var bRemoveSetType = bSetType1.RemoveSetTypeByName(typeof(int));
        var bClearSetType = bSetType1.ClearSetTypeByNames();
        Assert.NotSame(bSetType1, bRemoveSetType);
        Assert.NotSame(bSetType1, bClearSetType);

        var bAddAllSet = b0.DoAddAllSetTypeByNameMethods();
        var bNotAddAllSet = bAddAllSet.DoNotAddAllSetTypeByNameMethods();
        Assert.NotSame(b0, bAddAllSet);
        Assert.NotSame(bAddAllSet, bNotAddAllSet);

        var bIndexer = b0.WithIndexer(setter: true);
        var bNoIndexer = bIndexer.WithoutIndexer();
        Assert.NotSame(b0, bIndexer);
        Assert.NotSame(bIndexer, bNoIndexer);

        var bCustomMethod = b0.AddCustomMethod("Foo", m => m.WithEmitter(il => il.Emit(OpCodes.Ret)));
        var bRemoveMethod = bCustomMethod.RemoveCustomMethods("Foo");
        var bClearMethods = bCustomMethod.ClearCustomMethods();
        Assert.NotSame(b0, bCustomMethod);
        Assert.NotSame(bCustomMethod, bRemoveMethod);
        Assert.NotSame(bCustomMethod, bClearMethods);

        var bParamless = b0.WithParameterlessConstructor();
        var bNoParamless = bParamless.WithoutParameterlessConstructor();
        Assert.NotSame(b0, bParamless);
        Assert.NotSame(bParamless, bNoParamless);

        var bMinimal = b0.WithMinimalConstructor();
        var bNoMinimal = bMinimal.WithoutMinimalConstructor();
        Assert.NotSame(b0, bMinimal);
        Assert.NotSame(bMinimal, bNoMinimal);

        var bFull = b0.WithFullConstructor();
        var bNoFull = bFull.WithoutFullConstructor();
        Assert.NotSame(b0, bFull);
        Assert.NotSame(bFull, bNoFull);

        var bEq = b0.WithEquality(operators: true);
        var bNoEq = bEq.WithoutEquality();
        Assert.NotSame(b0, bEq);
        Assert.NotSame(bEq, bNoEq);

        var bCaseSens = b0.CaseSensitive();
        var bCaseInsens = b0.CaseInsensitive();
        Assert.NotSame(b0, bCaseSens);
        Assert.NotSame(b0, bCaseInsens);
    }

    [Fact]
    public void AddField_DuplicateName_ThrowsArgumentException()
    {
        var builder = EasyTypeBuilder.Create().AddField("Name", typeof(string));
        Assert.Throws<ArgumentException>(() => builder.AddField("Name", typeof(int)));
    }

    [Fact]
    public void Equality_And_GetHashCode_WorkCorrectly()
    {
        var b1 = (EasyTypeBuilder)EasyTypeBuilder.Create("SameType").AddField("f1", typeof(int));
        var b2 = (EasyTypeBuilder)EasyTypeBuilder.Create("SameType").AddField("f1", typeof(int));
        var b3 = (EasyTypeBuilder)EasyTypeBuilder.Create("DifferentType");

        Assert.True(b1.Equals(b2));
        Assert.True(b1.Equals((object)b2));
        Assert.True(b1.Equals(b1));
        Assert.False(b1.Equals(null));
        Assert.False(b1.Equals((object?)null));
        Assert.False(b1.Equals(b3));
        Assert.False(b1.Equals("other"));

        Assert.Equal(b1.GetHashCode(), b2.GetHashCode());

        Assert.True(b1 == b2);
        Assert.False(b1 != b2);
        Assert.False(b1 == b3);
        Assert.True(b1 != b3);

        EasyTypeBuilder? nullB1 = null;
        EasyTypeBuilder? nullB2 = null;
        Assert.True(nullB1 == nullB2);
        Assert.False(nullB1 != nullB2);
        Assert.False(b1 == nullB1);
        Assert.True(b1 != nullB1);
    }

    [Fact]
    public void Build_ReturnsValidConstructedType()
    {
        var type = EasyTypeBuilder.Create().Defaults().Build();
        Assert.NotNull(type);
        Assert.True(type.IsClass);
    }
}
