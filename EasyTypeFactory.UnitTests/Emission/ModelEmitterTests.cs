using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Emission;
using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Emission;

public class ModelEmitterTests
{
    private static ModuleBuilder CreateModuleBuilder()
    {
        var assemblyName = new AssemblyName("TestAssembly_" + Guid.NewGuid().ToString("N"));
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        return assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
    }

    [Fact]
    public void Create_WithAllMembers_EmitsAndInstantiatesType()
    {
        var moduleBuilder = CreateModuleBuilder();

        var fId = new ModelField { Name = "_id", OriginalName = "Id", Type = typeof(int), Required = true };
        var fName = new ModelField { Name = "_name", OriginalName = "Name", Type = typeof(string), ReadOnly = false };

        var propId = new ModelProperty { Name = "Id", Field = fId, HasSet = false };
        var propName = new ModelProperty { Name = "Name", Field = fName, HasSet = true };

        var ctor = new ModelConstructor { Fields = [fId, fName], SetsRequiredMembers = true };
        var eqTypeMethod = new ModelEqualsTypeMethod();
        var eqObjMethod = new ModelEqualsObjectMethod();
        var getHashMethod = new ModelGetHashCodeMethod();
        var getByNameMethod = new ModelGetByNameMethod { Name = "GetByName" };
        var getTByNameMethod = new ModelGetTByNameMethod { Name = "GetTByName" };
        var getTypeByNameMethod = new ModelGetTypeByNameMethod { Name = "GetStringByName", ReturnType = typeof(string) };
        var setByNameMethod = new ModelSetByNameMethod { Name = "SetByName" };
        var setTByNameMethod = new ModelSetTByNameMethod { Name = "SetTByName" };
        var setTypeByNameMethod = new ModelSetTypeByNameMethod { Name = "SetStringByName", Parameters = [typeof(string), typeof(string)] };
        var customMethod = new ModelCustomMethod
        {
            Name = "GetPrefix",
            Attributes = MethodAttributes.Public | MethodAttributes.HideBySig,
            ReturnType = typeof(string),
            Parameters = Type.EmptyTypes,
            MethodModifiers = [],
            Emitter = (il, ctx) =>
            {
                il.Emit(OpCodes.Ldstr, "PREFIX_");
                il.Emit(OpCodes.Ret);
            }
        };

        var opEq = new ModelEqualityOperator();
        var opIneq = new ModelInequalityOperator();

        var model = new global::EasyTypeFactory.Model.Model
        {
            Name = "Company.Product.MyGeneratedType",
            DiscriminateName = true,
            Fields = [fId, fName],
            Properties = [propId, propName],
            Indexer = new ModelIndexer { HasSet = true },
            Constructors = [ctor],
            Interfaces = [typeof(IEquatable<>)],
            BaseType = typeof(object),
            Methods = [
                eqTypeMethod,
                eqObjMethod,
                getHashMethod,
                getByNameMethod,
                getTByNameMethod,
                getTypeByNameMethod,
                setByNameMethod,
                setTByNameMethod,
                setTypeByNameMethod,
                customMethod
            ],
            Operators = [opEq, opIneq],
            CaseInsensitive = true
        };

        var emitter = new ModelEmitter(model, moduleBuilder);
        var type = emitter.Create();

        Assert.NotNull(type);
        Assert.StartsWith("Company.Product.MyGeneratedType<", type.FullName!);

        var instance = Activator.CreateInstance(type, [1, "Initial"])!;
        Assert.NotNull(instance);

        Assert.Equal(1, type.GetProperty("Id")!.GetValue(instance));
        Assert.Equal("Initial", type.GetProperty("Name")!.GetValue(instance));

        var customMethodInfo = type.GetMethod("GetPrefix")!;
        Assert.Equal("PREFIX_", customMethodInfo.Invoke(instance, null));
    }

    [Fact]
    public void Create_WithoutDiscriminateName_ThrowsDuplicateNameExceptionWhenRepeated()
    {
        var moduleBuilder = CreateModuleBuilder();
        var model = new global::EasyTypeFactory.Model.Model
        {
            Name = "UniqueTypeInModule",
            DiscriminateName = false,
            Fields = [],
            Properties = [],
            Indexer = null,
            Constructors = [new ModelConstructor { Fields = [] }],
            Interfaces = [],
            BaseType = typeof(object),
            Methods = [],
            Operators = [],
            CaseInsensitive = false
        };

        var emitter1 = new ModelEmitter(model, moduleBuilder);
        var type1 = emitter1.Create();
        Assert.Equal("UniqueTypeInModule", type1.FullName);

        var emitter2 = new ModelEmitter(model, moduleBuilder);
        Assert.Throws<DuplicateNameException>(() => emitter2.Create());
    }

    [Fact]
    public void Create_WithNullName_UsesDefaultNamespaceAndClassName()
    {
        var moduleBuilder = CreateModuleBuilder();
        var model = new global::EasyTypeFactory.Model.Model
        {
            Name = null,
            DiscriminateName = true,
            Fields = [],
            Properties = [],
            Indexer = null,
            Constructors = [new ModelConstructor { Fields = [] }],
            Interfaces = [],
            BaseType = typeof(object),
            Methods = [],
            Operators = [],
            CaseInsensitive = false
        };

        var emitter = new ModelEmitter(model, moduleBuilder);
        var type = emitter.Create();
        Assert.StartsWith("EasyTypeFactory.Generated.Anonymous<", type.FullName!);
    }
}
