using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Model;
using static System.Reflection.Emit.OpCodes;

namespace EasyTypeFactory.Emission;

internal sealed class ConstructorEmitter : IEmitter
{
    private readonly ModelConstructor _constructor;
    private readonly TypeBuilder _builder;
    private readonly Dictionary<ModelField, FieldBuilder> _fields;

    public ConstructorEmitter(
        TypeBuilder builder,
        ModelConstructor constructor,
        Dictionary<ModelField, FieldBuilder> fields)
    {
        _builder = builder;
        _constructor = constructor;
        _fields = fields;
    }

    public void Emit()
    {
        var types = _constructor.Fields.Select(field => field.Type).ToArray();
        
        var constructorBuilder = _builder.DefineConstructor(
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | 
            MethodAttributes.RTSpecialName,
            CallingConventions.Standard,
            types);

        // parameters start at 1 because 0 is the return parameter.
        for (var i = 1; i <= _constructor.Fields.Length; i++)
        {
            constructorBuilder.DefineParameter(i, ParameterAttributes.None, _constructor.Fields[i - 1].OriginalName);
        }

        if (_constructor.SetsRequiredMembers)
        {
            constructorBuilder.SetCustomAttribute(Utils.SetsRequiredMembersAttribute);
        }
        
        var initializeBeforeBaseCall = new List<(FieldBuilder, object?)>();
        var fieldsFromArguments = new List<(FieldBuilder, int)>();

        foreach (var (finalModelField, fieldBuilder) in _fields)
        {
#if NET10_0_OR_GREATER
            var argumentIndex = _constructor.Fields.IndexOf(finalModelField);
#else
            var argumentIndex = Array.IndexOf(_constructor.Fields, finalModelField);
#endif

            if (argumentIndex >= 0)
            {
                fieldsFromArguments.Add((fieldBuilder, argumentIndex + 1));
                
                if (!fieldBuilder.IsInitOnly)
                {
                    continue;
                }
                
                // initonly fields with arguments still need a placeholder before base() to satisfy the CLR, then get
                // overwritten by the argument after.
                
                var value = GetDefault(finalModelField.Type);
                initializeBeforeBaseCall.Add((fieldBuilder, value));
            }
            else if (finalModelField.DefaultValue is not null)
            {
                initializeBeforeBaseCall.Add((fieldBuilder, finalModelField.DefaultValue));
            }
        }
        
        var baseType = _builder.BaseType ?? typeof(object);
        var constructor = baseType.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null)
            ?? throw new InvalidOperationException($"Base type {baseType} does not have a parameterless constructor.");
        
        var il = constructorBuilder.GetILGenerator();
        
        // Initialize fields before base constructor call

        foreach (var (field, value) in initializeBeforeBaseCall)
        {
            if (field.FieldType.IsValueType && (value is null || Equals(value, Activator.CreateInstance(field.FieldType))))
            {
                il.Emit(Ldarg_0);
                il.Emit(Ldflda, field);
                il.Emit(Initobj, field.FieldType);
            }
            else
            {
                il.Emit(Ldarg_0);
                EmitDefaultValue(il, value);
                il.Emit(Stfld, field);
            }
        }
        
        // Call base constructor
        
        il.Emit(Ldarg_0);
        il.Emit(Call, constructor);
        
        // Set fields from arguments

        foreach (var (field, argumentIndex) in fieldsFromArguments)
        {
            il.Emit(Ldarg_0);
            il.Emit(Ldarg, argumentIndex);
            il.Emit(Stfld, field);
        }
        
        il.Emit(Ret);
    }

    private static object? GetDefault(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    private static void EmitDefaultValue(ILGenerator il, object? value)
    {
        switch (value)
        {
            case null:
                il.Emit(Ldnull);
                break;
            
            case bool b:
                il.Emit(b ? Ldc_I4_1 : Ldc_I4_0);
                break;

            case byte b:
                il.Emit(Ldc_I4_S, (sbyte)b);
                break;

            case sbyte b:
                il.Emit(Ldc_I4_S, b);
                break;

            case short s:
                il.Emit(Ldc_I4, (int)s);
                break;

            case ushort s:
                il.Emit(Ldc_I4, s);
                break;

            case int i:
                il.Emit(Ldc_I4, i);
                break;

            case uint u:
                il.Emit(Ldc_I4, (int)u);
                break;

            case long l:
                il.Emit(Ldc_I8, l);
                break;

            case ulong u:
                il.Emit(Ldc_I8, (long)u);
                break;

            case float f:
                il.Emit(Ldc_R4, f);
                break;

            case double d:
                il.Emit(Ldc_R8, d);
                break;

            case char c:
                il.Emit(Ldc_I4, c);
                break;

            case string s:
                il.Emit(Ldstr, s);
                break;

            case Enum e:
                value = Convert.ChangeType(e, Enum.GetUnderlyingType(e.GetType()), CultureInfo.InvariantCulture);
                EmitDefaultValue(il, value);
                break;

            default:
                throw new NotSupportedException(
                    $"Cannot emit default value for type {value.GetType()}. Only primitive types are supported.");
        }
    }
}
