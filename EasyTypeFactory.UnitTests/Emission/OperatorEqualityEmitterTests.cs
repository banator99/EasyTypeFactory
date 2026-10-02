using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Emission;
using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Emission;

public class OperatorEqualityEmitterTests
{
    private static (TypeBuilder TypeBuilder, MethodBuilder EqualsMethod) CreateTypeBuilderWithEquals()
    {
        var assemblyName = new AssemblyName("TestAssembly_" + Guid.NewGuid().ToString("N"));
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        var typeBuilder = moduleBuilder.DefineType("TestType_" + Guid.NewGuid().ToString("N"), TypeAttributes.Public | TypeAttributes.Class);

        var modelField = new ModelField { Name = "_id", OriginalName = "Id", Type = typeof(int) };
        var fieldBuilder = typeBuilder.DefineField("_id", typeof(int), FieldAttributes.Public);
        var fields = new Dictionary<ModelField, FieldBuilder> { [modelField] = fieldBuilder };

        var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
        var il = ctor.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
        il.Emit(OpCodes.Ret);

        var equalsEmitter = new EqualsTypeEmitter(typeBuilder, fields);
        equalsEmitter.Emit();

        return (typeBuilder, equalsEmitter.Method!);
    }

    [Fact]
    public void Emit_OperatorEquality_ComparesBothNullLeftNullRightNullAndNonNull()
    {
        var (typeBuilder, equalsMethod) = CreateTypeBuilderWithEquals();

        var opEmitter = new OperatorEqualityEmitter(typeBuilder, equalsMethod);
        opEmitter.Emit();

        Assert.NotNull(opEmitter.Method);

        var generatedType = typeBuilder.CreateType();
        var instance1 = Activator.CreateInstance(generatedType)!;
        var instance2 = Activator.CreateInstance(generatedType)!;
        var instance3 = Activator.CreateInstance(generatedType)!;

        var idField = generatedType.GetField("_id")!;
        idField.SetValue(instance1, 100);
        idField.SetValue(instance2, 100);
        idField.SetValue(instance3, 200);

        var opEquality = generatedType.GetMethod("op_Equality", BindingFlags.Public | BindingFlags.Static)!;
        Assert.NotNull(opEquality);

        // both null -> true
        Assert.True((bool)opEquality.Invoke(null, [null, null])!);

        // left null, right not null -> false
        Assert.False((bool)opEquality.Invoke(null, [null, instance1])!);

        // left not null, right null -> false
        Assert.False((bool)opEquality.Invoke(null, [instance1, null])!);

        // equal -> true
        Assert.True((bool)opEquality.Invoke(null, [instance1, instance2])!);

        // not equal -> false
        Assert.False((bool)opEquality.Invoke(null, [instance1, instance3])!);
    }
}
