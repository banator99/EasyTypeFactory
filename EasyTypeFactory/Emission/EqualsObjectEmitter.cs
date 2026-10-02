using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Model;
using static System.Reflection.Emit.OpCodes;

namespace EasyTypeFactory.Emission;

internal sealed class EqualsObjectEmitter : IEmitter
{
    private readonly TypeBuilder _builder;
    private readonly Dictionary<ModelField, FieldBuilder> _fields;

    public EqualsObjectEmitter(TypeBuilder builder, Dictionary<ModelField, FieldBuilder> fields)
    {
        _builder = builder;
        _fields = fields;
    }
    
    public void Emit()
    {
        var method = _builder.DefineMethod(
            "Equals", 
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
            typeof(bool),
            [typeof(object)]);
        
        var il = method.GetILGenerator();
        var other = il.DeclareLocal(_builder);
        var compatible = il.DefineLabel();
        
        il.Emit(Ldarg_1);
        il.Emit(Isinst, _builder);
        il.Emit(Stloc, other);
        il.Emit(Ldloc, other);
        il.Emit(Brtrue_S, compatible);
        il.Emit(Ldc_I4_0);
        il.Emit(Ret);
        il.MarkLabel(compatible);
        il.Emit(Ldloc, other);
        il.Emit(Starg_S, 1);
        
        EqualsTypeEmitter.EmitFieldComparisons(il, _fields);
        _builder.DefineMethodOverride(method, Utils.ObjectEqualsMethod);
    }
}
