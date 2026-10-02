using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Model;
using static System.Reflection.Emit.OpCodes;

namespace EasyTypeFactory.Emission;

internal sealed class SetByNameEmitter : IEmitter
{
    private static readonly MethodInfo ObjectGetTypeMethod = typeof(object).GetMethod(nameof(GetType))!;
    private static readonly MethodInfo ObjectToStringMethod = typeof(object).GetMethod(nameof(ToString))!;
    
    private readonly ModelSetByNameMethod _method;
    private readonly TypeBuilder _builder;
    private readonly Dictionary<ModelField, FieldBuilder> _fields;
    private readonly bool _caseInsensitive;
    
    public SetByNameEmitter(
        ModelSetByNameMethod method,
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
    /// public void SetByName(string s, object? value)
    /// {
    ///     if ("s".Equals(s)) 
    ///     {
    ///         if (value is string str)
    ///         {
    ///             this.s = str;
    ///             return;
    ///         }
    ///         if (value is null)
    ///         {
    ///             this.s = null!;
    ///             return;
    ///         }
    ///         throw new InvalidOperationException("Type of value must be string, "+value.GetType().ToString()+" given");
    ///     }
    ///     if ("s".Equals(s)) 
    ///     {
    ///         if (value is DateTime dt)
    ///         {
    ///             this._writableDt = dt;
    ///             return;
    ///         }
    ///         throw new InvalidOperationException("Type of value must be string, .. given");
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
        var method = _builder.DefineMethod(
            _method.Name,
            MethodAttributes.Public | MethodAttributes.HideBySig,
            typeof(void),
            [typeof(string), typeof(object)]);
        
        method.DefineParameter(1, ParameterAttributes.None, "name");
        method.DefineParameter(2, ParameterAttributes.None, "value");
        
        var il = method.GetILGenerator();
        
        EmitToILGenerator(il, _fields, _caseInsensitive);
    }

    public static void EmitToILGenerator(
        ILGenerator il,
        Dictionary<ModelField, FieldBuilder> fields,
        bool caseInsensitive)
    {
        // FIXME: use ors (||) for the "Field .. is not settable" exception.
        
        var locals = new Dictionary<Type, LocalBuilder>();
        
        var markNameFalseLabel = false;
        Label nameFalseLabel = default;

        foreach (var (field, fieldBuilder) in fields)
        {
            if (markNameFalseLabel)
            {
                il.MarkLabel(nameFalseLabel);
            }
            
            var typeFalseLabel = il.DefineLabel();
            
            nameFalseLabel = il.DefineLabel();
            markNameFalseLabel = true;

            if (!locals.TryGetValue(field.Type, out var typedValue))
            {
                typedValue = il.DeclareLocal(field.Type);
                locals.Add(field.Type, typedValue);
            }
            
            Utils.EmitStringEquals(il, 1, field.OriginalName, caseInsensitive);
            il.Emit(Brfalse_S, nameFalseLabel);

            if (field.ReadOnly)
            {
                il.Emit(Ldstr, $"Cannot set field '{field}' because it's read-only.");
                il.Emit(Newobj, Utils.InvalidOperationExceptionCtor);
                il.Emit(Throw);
            }
            else
            {
                // if (value is string str)
                il.Emit(Ldarg_2);
                il.Emit(Isinst, field.Type);

                if (!field.Type.IsValueType)
                {
                    // When it's a reference type we can store the result of Isinst in a local
                    // This saves us from an extra cast later.
                    il.Emit(Stloc, typedValue);
                    il.Emit(Ldloc, typedValue);
                }
                
                il.Emit(Brfalse_S, typeFalseLabel);
                
                if (field.Type.IsValueType)
                {
                    // Value types must be unboxed because they're delivered as an object
                    il.Emit(Ldarg_2);
                    il.Emit(Unbox_Any, field.Type);
                    il.Emit(Stloc, typedValue);
                }
                
                il.Emit(Ldarg_0);
                il.Emit(Ldloc, typedValue);
                il.Emit(Stfld, fieldBuilder);
                il.Emit(Ret);
                
                il.MarkLabel(typeFalseLabel);

                if (!field.Type.IsValueType)
                {
                    var trueLabel = il.DefineLabel();
                    
                    // Reference types must also have a null check since isinst returns false for nulls
                    // if (value is null)
                    
                    il.Emit(Ldarg_2);
                    il.Emit(Brtrue_S, trueLabel); // if true, it's not null -> type is something else; throw exception
                    
                    il.Emit(Ldarg_0);
                    il.Emit(Ldnull);
                    il.Emit(Stfld, fieldBuilder);
                    il.Emit(Ret);
                    
                    il.MarkLabel(trueLabel);
                }
                
                il.Emit(
                    Ldstr,
                    $"Cannot set field '{field}' because its type '{field.Type}' is not compatible with the type of the " +
                    $"value '");
                
                il.Emit(Ldarg_2);
                il.Emit(Callvirt, ObjectGetTypeMethod);
                il.Emit(Callvirt, ObjectToStringMethod);
                il.Emit(Ldstr, "'.");
                il.Emit(Call, Utils.StringConcat3Method);
                il.Emit(Newobj, Utils.InvalidOperationExceptionCtor);
                il.Emit(Throw);   
            }
        }

        if (markNameFalseLabel)
        {
            il.MarkLabel(nameFalseLabel);
        }

        Utils.EmitThrowFieldNotFoundException(il, 1);
    }
}
