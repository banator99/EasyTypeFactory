using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Model;
using static System.Reflection.Emit.OpCodes;

namespace EasyTypeFactory.Emission;

internal sealed class GetTypeByNameEmitter : IEmitter
{
    private readonly ModelGetTypeByNameMethod _method;
    private readonly TypeBuilder _builder;
    private readonly Dictionary<ModelField, FieldBuilder> _fields;
    private readonly bool _caseInsensitive;
    
    public GetTypeByNameEmitter(
        ModelGetTypeByNameMethod method,
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
    /// public string GetStringByName(string s)
    /// {
    ///     if ("s".Equals(s)) 
    ///     {
    ///         return this.s;
    ///     }
    ///     if ("DT".Equals(s) || "else".Equals(s))
    ///     {
    ///         throw new InvalidOperationException("Type not supported");
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
            _method.ReturnType,
            [typeof(string)]);
        
        method.DefineParameter(1, ParameterAttributes.None, "name");

        var supportedFields = new List<KeyValuePair<ModelField, FieldBuilder>>();
        var unsupportedFields = new List<string>();

        foreach (var kvp in _fields)
        {
            if (kvp.Key.Type == _method.ReturnType)
            {
                supportedFields.Add(kvp);
            }
            else
            {
                unsupportedFields.Add(kvp.Key.OriginalName);
            }
        }
        
        var il = method.GetILGenerator();

        var markFalseLabel = false;
        Label falseLabel = default;

        foreach (var (field, fieldBuilder) in supportedFields)
        {
            if (markFalseLabel)
            {
                il.MarkLabel(falseLabel);
            }
            
            falseLabel = il.DefineLabel();
            markFalseLabel = true;
            
            Utils.EmitStringEquals(il, 1, field.OriginalName, _caseInsensitive);
            il.Emit(Brfalse_S, falseLabel);
            il.Emit(Ldarg_0);
            il.Emit(Ldfld, fieldBuilder);
            il.Emit(Ret);
        }

        if (unsupportedFields.Count > 0)
        {
            if (markFalseLabel)
            {
                il.MarkLabel(falseLabel);
            }
            
            falseLabel = il.DefineLabel();
            markFalseLabel = true;
            
            var trueLabel = il.DefineLabel();

            for (var i = 0; i < unsupportedFields.Count; i++)
            {
                Utils.EmitStringEquals(il, 1, unsupportedFields[i], _caseInsensitive);

                if (i == unsupportedFields.Count - 1)
                {
                    il.Emit(Brfalse, falseLabel);
                }
                else
                {
                    il.Emit(Brtrue, trueLabel);
                }
            }

            il.MarkLabel(trueLabel);
            
            Utils.EmitThrowFieldTypeNotCompatibleException(il, 1, _method.ReturnType);
        }

        if (markFalseLabel)
        {
            il.MarkLabel(falseLabel);
        }

        Utils.EmitThrowFieldNotFoundException(il, 1);
    }
}