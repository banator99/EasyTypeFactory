using System;
using System.Reflection.Emit;
using EasyTypeFactory.Model;

namespace EasyTypeFactory.Emission;

internal class CustomMethodEmitter : IEmitter
{
    private readonly TypeBuilder _builder;
    private readonly ModelCustomMethod _method;

    public CustomMethodEmitter(TypeBuilder builder, ModelCustomMethod method)
    {
        _builder = builder;
        _method = method;
    }
    
    public void Emit()
    {
        var method = _builder.DefineMethod(
            _method.Name,
            _method.Attributes,
            _method.ReturnType,
            _method.Parameters ?? Type.EmptyTypes);

        var context = new MethodBuilderContext(method);
        
        foreach (var modifier in _method.MethodModifiers)
        {
            modifier(method, context);
        }
        
        var il = method.GetILGenerator();
        
        _method.Emitter(il, context);
    }
}
