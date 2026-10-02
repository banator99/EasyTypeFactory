using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Model;

namespace EasyTypeFactory.Emission;

internal sealed class IndexerEmitter : IEmitter
{
    private const MethodAttributes PropertyMethodAttributes =
        MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot |
        MethodAttributes.SpecialName;

    private readonly ModelIndexer _indexer;
    private readonly TypeBuilder _builder;
    private readonly Dictionary<ModelField, FieldBuilder> _fields;
    private readonly bool _caseInsensitive;

    public IndexerEmitter(ModelIndexer indexer, TypeBuilder builder, Dictionary<ModelField, FieldBuilder> fields, bool caseInsensitive)
    {
        _indexer = indexer;
        _builder = builder;
        _fields = fields;
        _caseInsensitive = caseInsensitive;
    }

    public void Emit()
    {
        var propertyBuilder = _builder.DefineProperty(
            "Item",
            PropertyAttributes.None,
            typeof(object),
            [typeof(string)]);

        EmitGetter(propertyBuilder);

        if (_indexer.HasSet)
        {
            EmitSetter(propertyBuilder);
        }
    }

    private void EmitGetter(PropertyBuilder propertyBuilder)
    {
        var method = _builder.DefineMethod(
            "get_Item",
            PropertyMethodAttributes,
            typeof(object),
            [typeof(string)]);

        method.DefineParameter(1, ParameterAttributes.None, "name");

        var il = method.GetILGenerator();
        GetByNameEmitter.EmitToILGenerator(il, _fields, _caseInsensitive);
        
        propertyBuilder.SetGetMethod(method);

        if (_indexer.OverriddenGetMethod is not null)
        {
            _builder.DefineMethodOverride(method, _indexer.OverriddenGetMethod);
        }
    }

    private void EmitSetter(PropertyBuilder propertyBuilder)
    {
        var method = _builder.DefineMethod(
            "set_Item",
            PropertyMethodAttributes,
            typeof(void),
            [typeof(string), typeof(object)]);

        method.DefineParameter(1, ParameterAttributes.None, "name");
        method.DefineParameter(2, ParameterAttributes.None, "value");

        var il = method.GetILGenerator();
        SetByNameEmitter.EmitToILGenerator(il, _fields, _caseInsensitive);
        
        propertyBuilder.SetSetMethod(method);

        if (_indexer.OverriddenSetMethod is not null)
        {
            _builder.DefineMethodOverride(method, _indexer.OverriddenSetMethod);
        }
    }
}
