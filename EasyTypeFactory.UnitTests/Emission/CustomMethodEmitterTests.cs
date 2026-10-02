using System;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Emission;
using EasyTypeFactory.Model;
using Xunit;

namespace EasyTypeFactory.UnitTests.Emission;

public class CustomMethodEmitterTests
{
    [Fact]
    public void Emit_CustomMethod_ExecutesILAndAppliesModifiers()
    {
        var assemblyName = new AssemblyName("TestAssembly_" + Guid.NewGuid().ToString("N"));
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        var typeBuilder = moduleBuilder.DefineType("TestType_" + Guid.NewGuid().ToString("N"), TypeAttributes.Public | TypeAttributes.Class);

        // Add default constructor
        var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
        var ctorIl = ctor.GetILGenerator();
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
        ctorIl.Emit(OpCodes.Ret);

        var modifierCalled = false;
        var customMethod = new ModelCustomMethod
        {
            Name = "AddNumbers",
            Attributes = MethodAttributes.Public | MethodAttributes.HideBySig,
            ReturnType = typeof(int),
            Parameters = [typeof(int), typeof(int)],
            MethodModifiers = [
                (mb, ctx) =>
                {
                    modifierCalled = true;
                    Assert.NotNull(mb);
                    Assert.NotNull(ctx);
                }
            ],
            Emitter = (il, ctx) =>
            {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Ret);
            }
        };

        var emitter = new CustomMethodEmitter(typeBuilder, customMethod);
        emitter.Emit();

        Assert.True(modifierCalled);

        var type = typeBuilder.CreateType();
        var instance = Activator.CreateInstance(type)!;

        var method = type.GetMethod("AddNumbers", [typeof(int), typeof(int)])!;
        Assert.NotNull(method);

        var result = (int)method.Invoke(instance, [15, 25])!;
        Assert.Equal(40, result);
    }
}
