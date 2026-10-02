using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using static System.Reflection.Emit.OpCodes;

namespace EasyTypeFactory.Emission;

internal sealed class OperatorEqualityEmitter : IEmitter
{
    private readonly TypeBuilder _builder;
    private readonly MethodInfo _equalsMethod;

    public MethodBuilder? Method { get; private set; }

    public OperatorEqualityEmitter(TypeBuilder builder, MethodInfo equalsMethod)
    {
        _builder = builder;
        _equalsMethod = equalsMethod;
    }
    
    /// <summary>
    /// Emits the following implementation:
    /// <code>
    /// public static bool operator ==(X? left, X? right)
    /// {
    ///     return left is null && right is null || left is not null && left.Equals(right);
    /// }
    /// </code>
    /// </summary>
    
    public void Emit()
    {
        var method = _builder.DefineMethod(
            "op_Equality",
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            typeof(bool),
            [_builder, _builder]);

        Method = method;
        
        var il = method.GetILGenerator();
        
        var callEquals = il.DefineLabel();
        var returnFalse = il.DefineLabel();
        var returnTrue = il.DefineLabel();
        
        // is left not null?
        il.Emit(Ldarg_0);
        il.Emit(Brtrue_S, callEquals);
        
        // left is null, is right null?
        il.Emit(Ldarg_1);
        il.Emit(Brfalse_S, returnTrue); // both null, return true
        il.Emit(Br_S, returnFalse);
        
        // call left.Equals(right)
        il.MarkLabel(callEquals);
        il.Emit(Ldarg_0);
        il.Emit(Ldarg_1);
        il.Emit(Callvirt, _equalsMethod);
        il.Emit(Ret);
        
        il.MarkLabel(returnFalse);
        il.Emit(Ldc_I4_0);
        il.Emit(Ret);
        
        il.MarkLabel(returnTrue);
        il.Emit(Ldc_I4_1);
        il.Emit(Ret);
    }
}
