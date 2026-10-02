using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Model;
using static System.Reflection.Emit.OpCodes;

namespace EasyTypeFactory.Emission;

internal sealed class SetTypeByNameEmitter : IEmitter
{
    private readonly ModelSetTypeByNameMethod _method;
    private readonly TypeBuilder _builder;
    private readonly Dictionary<ModelField, FieldBuilder> _fields;
    private readonly bool _caseInsensitive;
    
    public SetTypeByNameEmitter(
        ModelSetTypeByNameMethod method,
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
    
    /// </code>
    /// </summary>
    public void Emit()
    {
        var valueType = _method.Parameters![1];
        var method = _builder.DefineMethod(
            _method.Name,
            MethodAttributes.Public | MethodAttributes.HideBySig,
            typeof(void),
            [typeof(string), valueType]);

        method.DefineParameter(1, ParameterAttributes.None, "name");
        method.DefineParameter(2, ParameterAttributes.None, "value");
        var il = method.GetILGenerator();
        var next = il.DefineLabel();
        var hasNext = false;

        foreach (var (field, fieldBuilder) in _fields)
        {
            if (hasNext)
            {
                il.MarkLabel(next);
            }

            next = il.DefineLabel();
            hasNext = true;
            Utils.EmitStringEquals(il, 1, field.OriginalName, _caseInsensitive);
            il.Emit(Brfalse, next);

            if (field.ReadOnly)
            {
                il.Emit(Ldstr, $"Cannot set field '{field.OriginalName}' because it's read-only.");
                il.Emit(Newobj, Utils.InvalidOperationExceptionCtor);
                il.Emit(Throw);
            }
            else if (field.Type == valueType)
            {
                il.Emit(Ldarg_0);
                il.Emit(Ldarg_2);
                il.Emit(Stfld, fieldBuilder);
                il.Emit(Ret);
            }
            else
            {
                Utils.EmitThrowFieldTypeNotCompatibleException(il, field.OriginalName, field.Type, valueType);
            }
        }

        if (hasNext)
        {
            il.MarkLabel(next);
        }

        Utils.EmitThrowFieldNotFoundException(il, 1);
    }
}
