using System;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Model;
using static System.Reflection.Emit.OpCodes;

namespace EasyTypeFactory.Emission;

internal sealed class PropertyEmitter : IEmitter
{
    private const MethodAttributes PropertyMethodAttributes = 
        MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot;
    
    private readonly ModelProperty _property;
    private readonly TypeBuilder _builder;
    private readonly FieldBuilder _backingField;
    private readonly string _explicitImplementationPrefix;
    
    public PropertyEmitter(
        TypeBuilder builder,
        ModelProperty property,
        FieldBuilder backingField)
    {
        _property = property;
        _builder = builder;
        _backingField = backingField;

        _explicitImplementationPrefix = _property.ExplicitImplementationType is not null
            ? Utils.GetFullName(_property.ExplicitImplementationType) + '.'
            : string.Empty;
    }

    public void Emit()
    {
        var propertyBuilder = _builder.DefineProperty(
            _explicitImplementationPrefix + _property.Name,
            PropertyAttributes.None,
            _property.Field.Type,
            _property.Field.ReadOnly
                ? null
                : [_property.Field.Type]);

        if (_property.Field.Required)
        {
            propertyBuilder.SetCustomAttribute(Utils.RequiredMemberAttribute);
        }
        
        EmitGetter(propertyBuilder);

        if (!_property.Field.ReadOnly)
        {
            EmitSetter(propertyBuilder);
        }
    }

    private void EmitGetter(PropertyBuilder propertyBuilder)
    {
        var method = _builder.DefineMethod(
            $"{_explicitImplementationPrefix}get_{_property.Name}",
            GetMethodAttributes(),
            _property.Field.Type,
            Type.EmptyTypes);
        
        var il = method.GetILGenerator();
        
        il.Emit(Ldarg_0);
        il.Emit(Ldfld, _backingField);
        il.Emit(Ret);
        
        propertyBuilder.SetGetMethod(method);

        if (_property.OverriddenGetMethod is not null)
        {
            _builder.DefineMethodOverride(method, _property.OverriddenGetMethod);
        }
    }

    private void EmitSetter(PropertyBuilder propertyBuilder)
    {
        var method = _builder.DefineMethod(
            $"{_explicitImplementationPrefix}set_{_property.Name}",
            GetMethodAttributes(),
            typeof(void),
            [_property.Field.Type]);
        
        var il = method.GetILGenerator();
        
        il.Emit(Ldarg_0);
        il.Emit(Ldarg_1);
        il.Emit(Stfld, _backingField);
        il.Emit(Ret);
        
        propertyBuilder.SetSetMethod(method);

        if (_property.OverriddenSetMethod is not null)
        {
            _builder.DefineMethodOverride(method, _property.OverriddenSetMethod);
        }
    }

    private MethodAttributes GetMethodAttributes()
    {
        return _property.ExplicitImplementationType is null
            ? PropertyMethodAttributes | MethodAttributes.Public
            : PropertyMethodAttributes | MethodAttributes.Private;
    }
}
