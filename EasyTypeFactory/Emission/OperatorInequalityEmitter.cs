using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using static System.Reflection.Emit.OpCodes;

namespace EasyTypeFactory.Emission;

internal sealed class OperatorInequalityEmitter : IEmitter
{
    private readonly TypeBuilder _builder;
    private readonly MethodInfo _opEqualityMethod;

    public OperatorInequalityEmitter(TypeBuilder builder, MethodInfo opEqualityMethod)
    {
        _builder = builder;
        _opEqualityMethod = opEqualityMethod;
    }

    /// <summary>
    /// Emits the following implementation:
    /// <code>
    /// public static bool operator !=(X? left, X? right)
    /// {
    ///     return !(left == right);
    /// }
    /// </code>
    /// </summary>
    public void Emit()
    {
        var method = _builder.DefineMethod(
            "op_Inequality",
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig, 
            typeof(bool),
            [_builder, _builder]);
        
        var il = method.GetILGenerator();
        
        il.Emit(Ldarg_0);
        il.Emit(Ldarg_1);
        il.Emit(Call, _opEqualityMethod);
        il.Emit(Ldc_I4_0);
        il.Emit(Ceq);
        il.Emit(Ret);
    }
}
