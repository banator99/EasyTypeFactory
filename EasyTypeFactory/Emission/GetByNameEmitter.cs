using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Model;
using static System.Reflection.Emit.OpCodes;

namespace EasyTypeFactory.Emission;

internal sealed class GetByNameEmitter : IEmitter
{
    private readonly ModelGetByNameMethod _method;
    private readonly TypeBuilder _builder;
    private readonly Dictionary<ModelField, FieldBuilder> _fields;
    private readonly bool _caseInsensitive;
    
    public GetByNameEmitter(
        ModelGetByNameMethod method,
        TypeBuilder builder,
        Dictionary<ModelField, FieldBuilder> fields,
        bool caseInsensitive)
    {
        _method = method;
        _builder = builder;
        _fields = fields;
        _caseInsensitive = caseInsensitive;
    }
    
    public void Emit()
    {
        var method = _builder.DefineMethod(
            _method.Name,
            MethodAttributes.Public | MethodAttributes.HideBySig,
            typeof(object),
            [typeof(string)]);
        
        method.DefineParameter(1, ParameterAttributes.None, "name");
        
        var il = method.GetILGenerator();
        EmitToILGenerator(il, _fields, _caseInsensitive);
    }

    public static void EmitToILGenerator(
        ILGenerator il,
        Dictionary<ModelField, FieldBuilder> fields,
        bool caseInsensitive)
    {
        var markFalseLabel = false;
        Label falseLabel = default;

        foreach (var (field, fieldBuilder) in fields)
        {
            if (markFalseLabel)
            {
                il.MarkLabel(falseLabel);
            }
            
            falseLabel = il.DefineLabel();
            markFalseLabel = true;
            
            Utils.EmitStringEquals(il, 1, field.OriginalName, caseInsensitive);
            il.Emit(Brfalse_S, falseLabel);
            il.Emit(Ldarg_0);
            il.Emit(Ldfld, fieldBuilder);

            if (field.Type.IsValueType)
            {
                il.Emit(Box, field.Type);
            }
            
            il.Emit(Ret);
        }

        if (markFalseLabel)
        {
            il.MarkLabel(falseLabel);
        }

        Utils.EmitThrowFieldNotFoundException(il, 1);
    }
}
