using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Model;
using static System.Reflection.Emit.OpCodes;

namespace EasyTypeFactory.Emission;

internal sealed class SetTByNameEmitter : IEmitter
{
    private readonly ModelSetTByNameMethod _method;
    private readonly TypeBuilder _builder;
    private readonly Dictionary<ModelField, FieldBuilder> _fields;
    private readonly bool _caseInsensitive;
    
    public SetTByNameEmitter(
        ModelSetTByNameMethod method,
        TypeBuilder builder,
        Dictionary<ModelField, FieldBuilder> fields,
        bool caseInsensitive)
    {
        _method = method;
        _builder = builder;
        _fields = fields;
        _caseInsensitive = caseInsensitive;
    }
    
    /// <summary>
    /// Emits the following method:
    /// <code>
    /// public void SetByName&lt;T&gt;(string s, T value)
    /// {
    ///     if ("s".Equals(s)) 
    ///     {
    ///         if (typeof(T) == typeof(string))
    ///         {
    ///             this.s = (string)(object)value!;
    ///             return;
    ///         }
    ///         if (value is string vstr)
    ///         {
    ///             this.s = vstr;
    ///             return;
    ///         }
    ///         throw new InvalidOperationException("Type of value must be string, " +typeof(T).ToString()+" given");
    ///     }
    ///     if ("s".Equals(s)) 
    ///     {
    ///         if (typeof(T) == typeof(DateTime))
    ///         {
    ///             this._writableDt = (DateTime)(object)value!;
    ///             return;
    ///         }
    ///         if (value is DateTime vdt)
    ///         {
    ///             this._writableDt = vdt;
    ///             return;
    ///         }
    ///         throw new InvalidOperationException("Type of value must be string, " +typeof(T).ToString()+" given");
    ///     }
    ///     if ("DT".Equals(s, StringComparison.OrdinalIgnoreCase)) 
    ///     {
    ///         throw new KeyNotFoundException("Field .. is not settable");
    ///     }
    ///     throw new KeyNotFoundException("No field with name '" + s  +"' was found.");
    /// }
    /// </code>
    /// </summary>
    public void Emit()
    {
        // FIXME: handle readonly fields
        
        var method = _builder.DefineMethod(
            _method.Name,
            MethodAttributes.Public | MethodAttributes.HideBySig);

        var generics = method.DefineGenericParameters("T");
        var genericT = generics[0];
        
        method.SetReturnType(typeof(void));
        method.SetParameters(typeof(string), genericT);
        
        method.DefineParameter(1, ParameterAttributes.None, "name");
        method.DefineParameter(2, ParameterAttributes.None, "value");
        
        var il = method.GetILGenerator();
        
        var locals = new Dictionary<Type, LocalBuilder>();
        
        var markNameFalseLabel = false;
        Label nameFalseLabel = default;

        foreach (var (field, fieldBuilder) in _fields)
        {
            if (markNameFalseLabel)
            {
                il.MarkLabel(nameFalseLabel);
            }
            
            nameFalseLabel = il.DefineLabel();
            markNameFalseLabel = true;
            
            var typeEqualsFalseLabel = il.DefineLabel();
            var typeIsAssignableFromFalseLabel = il.DefineLabel();
            
            if (!locals.TryGetValue(field.Type, out var typedValue))
            {
                typedValue = il.DeclareLocal(field.Type);
                locals.Add(field.Type, typedValue);
            }
            
            // if (name == "MyField")
            Utils.EmitStringEquals(il, 1, field.OriginalName, _caseInsensitive);
            il.Emit(Brfalse_S, nameFalseLabel);
            
            // if (typeof(T) == typeof(MyField))
            Utils.EmitTypeEqualsT(il, genericT, field.Type);
            il.Emit(Brfalse_S, typeEqualsFalseLabel);
            
            il.Emit(Ldarg_0);
            il.Emit(Ldarg_2);
            il.Emit(Stfld, fieldBuilder);
            il.Emit(Ret);
            
            // if (value is string valueStr)
            il.MarkLabel(typeEqualsFalseLabel);
            il.Emit(Ldarg_2);
            il.Emit(Box, genericT);
            il.Emit(Isinst, field.Type);

            if (!field.Type.IsValueType)
            {
                // When it's a reference type we can store the result of Isinst in a local
                // This saves us from an extra cast later.   
                il.Emit(Stloc, typedValue);
                il.Emit(Ldloc, typedValue);
            }

            il.Emit(Brfalse_S, typeIsAssignableFromFalseLabel);

            if (field.Type.IsValueType)
            {
                // Isinst only works with boxed values, which means that we need to box and unbox the values transmitted
                // with the Isinst call. This only applies to value types.
                il.Emit(Ldarg_2);
                il.Emit(Box, genericT);
                il.Emit(Isinst, field.Type);
                il.Emit(Unbox_Any, field.Type);
                il.Emit(Stloc, typedValue);
            }
            
            il.Emit(Ldarg_0);
            il.Emit(Ldloc, typedValue);
            il.Emit(Stfld, fieldBuilder);
            il.Emit(Ret);
            
            il.MarkLabel(typeIsAssignableFromFalseLabel);
            Utils.EmitThrowFieldTypeNotCompatibleException(il, field.OriginalName, field.Type, genericT);
        }

        if (markNameFalseLabel)
        {
            il.MarkLabel(nameFalseLabel);
        }

        Utils.EmitThrowFieldNotFoundException(il, 1);
    }
}
