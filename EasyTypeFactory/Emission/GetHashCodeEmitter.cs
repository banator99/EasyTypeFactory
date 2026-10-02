using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Model;

namespace EasyTypeFactory.Emission;

internal sealed class GetHashCodeEmitter : IEmitter
{
    private readonly TypeBuilder _builder;
    private readonly Dictionary<ModelField, FieldBuilder> _fields;

    public GetHashCodeEmitter(TypeBuilder builder, Dictionary<ModelField, FieldBuilder> fields)
    {
        _builder = builder;
        _fields = fields;
    }
    
    public void Emit()
    {
        var method = _builder.DefineMethod(
            "GetHashCode", 
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
            typeof(int),
            Type.EmptyTypes);
        
        var il = method.GetILGenerator();
        var hashCode = il.DeclareLocal(typeof(HashCode));
        
        il.Emit(OpCodes.Ldloca_S, hashCode);
        il.Emit(OpCodes.Initobj, typeof(HashCode));
        foreach (var (field, fieldBuilder) in _fields)
        {
            il.Emit(OpCodes.Ldloca_S, hashCode);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, fieldBuilder);
            il.Emit(OpCodes.Call, Utils.HashCodeGenericAddMethod.MakeGenericMethod(field.Type));
        }
        il.Emit(OpCodes.Ldloca_S, hashCode);
        il.Emit(OpCodes.Call, Utils.HashCodeFinishMethod);
        il.Emit(OpCodes.Ret);
        _builder.DefineMethodOverride(method, Utils.ObjectGetHashCodeMethod);
    }
}
