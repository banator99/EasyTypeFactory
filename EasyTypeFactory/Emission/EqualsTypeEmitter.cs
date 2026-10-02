using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Model;
using static System.Reflection.Emit.OpCodes;

namespace EasyTypeFactory.Emission;

internal sealed class EqualsTypeEmitter : IEmitter
{
    private readonly TypeBuilder _builder;
    private readonly Dictionary<ModelField, FieldBuilder> _fields;

    public MethodBuilder? Method { get; private set; }

    public EqualsTypeEmitter(TypeBuilder builder, Dictionary<ModelField, FieldBuilder> fields)
    {
        _builder = builder;
        _fields = fields;
    }

    public void Emit()
    {
        var method = _builder.DefineMethod(
            "Equals",
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final |
            MethodAttributes.HideBySig | MethodAttributes.NewSlot,
            typeof(bool),
            [_builder]);

        Method = method;

        method.DefineParameter(1, ParameterAttributes.None, "other");
        var il = method.GetILGenerator();
        var notNull = il.DefineLabel();
        il.Emit(Ldarg_1);
        il.Emit(Brtrue_S, notNull);
        il.Emit(Ldc_I4_0);
        il.Emit(Ret);
        il.MarkLabel(notNull);
        EmitFieldComparisons(il, _fields);
    }

    internal static void EmitFieldComparisons(ILGenerator il, Dictionary<ModelField, FieldBuilder> fields)
    {
        var returnFalse = il.DefineLabel();

        foreach (var (field, fieldBuilder) in fields)
        {
            var comparerType = typeof(EqualityComparer<>).MakeGenericType(field.Type);
            var getDefault = comparerType.GetProperty("Default", BindingFlags.Public | BindingFlags.Static)!.GetMethod!;
            var equals = comparerType.GetMethod("Equals", [field.Type, field.Type])!;

            il.Emit(Call, getDefault);
            il.Emit(Ldarg_0);
            il.Emit(Ldfld, fieldBuilder);
            il.Emit(Ldarg_1);
            il.Emit(Ldfld, fieldBuilder);
            il.Emit(Callvirt, equals);
            il.Emit(Brfalse, returnFalse);
        }

        il.Emit(Ldc_I4_1);
        il.Emit(Ret);
        il.MarkLabel(returnFalse);
        il.Emit(Ldc_I4_0);
        il.Emit(Ret);
    }
}
