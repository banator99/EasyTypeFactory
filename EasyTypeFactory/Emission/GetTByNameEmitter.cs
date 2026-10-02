using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Model;
using static System.Reflection.Emit.OpCodes;

namespace EasyTypeFactory.Emission;

internal sealed class GetTByNameEmitter : IEmitter
{
    private readonly ModelGetTByNameMethod _method;
    private readonly TypeBuilder _builder;
    private readonly Dictionary<ModelField, FieldBuilder> _fields;
    private readonly bool _caseInsensitive;
    
    public GetTByNameEmitter(
        ModelGetTByNameMethod method,
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
    /// This method emits the following method:
    /// <code>
    /// public T GetByName&lt;T&gt;(string s)
    /// {
    ///     if ("DT".Equals(s)) 
    ///     {
    ///         if (typeof(T) == typeof(DateTime))
    ///         {
    ///             // The jitter will devirtualize this branch completely. Since we now that T is the type we need here
    ///             // we can just return the field without any boxing. Please note that this is not supported in the C#
    ///             // language.
    ///             return this.DT;
    ///         }
    ///         if (typeof(DateTime).IsAssignableTo(typeof(T)) )
    ///         {
    ///             // Devirtualization is not guaranteed here. In this case, we'll perform boxing to make sure the type
    ///             // aligns with T.
    ///             return (T)(object)this.DT;
    ///         }
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
            MethodAttributes.Public | MethodAttributes.HideBySig);
        
        var generics = method.DefineGenericParameters("T");
        var genericT = generics[0];

        method.SetReturnType(genericT);
        method.SetParameters(typeof(string));
        method.DefineParameter(1, ParameterAttributes.None, "name");
        
        var il = method.GetILGenerator();
        
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
            
            // if (name == "MyField")
            Utils.EmitStringEquals(il, 1, field.OriginalName, _caseInsensitive);
            il.Emit(Brfalse_S, nameFalseLabel);
            
            // if (typeof(T) == typeof(MyField))
            Utils.EmitTypeEqualsT(il, genericT, field.Type);
            il.Emit(Brfalse_S, typeEqualsFalseLabel);
            
            il.Emit(Ldarg_0);
            il.Emit(Ldfld, fieldBuilder);
            il.Emit(Ret);
            
            // if (typeof(MyField).IsAssignableFrom(typeof(T)))
            il.MarkLabel(typeEqualsFalseLabel);
            Utils.EmitTypeFromHandle(il, genericT);
            Utils.EmitTypeFromHandle(il, field.Type);
            il.Emit(Call, Utils.TypeIsAssignableFromMethod);
            il.Emit(Brfalse_S, typeIsAssignableFromFalseLabel);
            
            il.Emit(Ldarg_0);
            il.Emit(Ldfld, fieldBuilder);
            il.Emit(Box, field.Type);
            il.Emit(Unbox_Any, genericT);
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
