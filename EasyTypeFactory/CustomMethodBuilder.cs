using System;
using System.Reflection;
using System.Reflection.Emit;
using EasyTypeFactory.Definition;

namespace EasyTypeFactory;

internal class CustomMethodBuilder : IEasyTypeMethodBuilder
{
    private readonly CustomMethodDefinition _definition;

    public CustomMethodBuilder(string name)
    {
        _definition = new CustomMethodDefinition(name);
    }

    private CustomMethodBuilder(CustomMethodDefinition definition)
    {
        _definition = definition;
    }
    
    public IEasyTypeMethodBuilder WithAttributes(MethodAttributes attributes)
    {
        return new CustomMethodBuilder(_definition with
        {
            Attributes = attributes
        });
    }

    public IEasyTypeMethodBuilder WithReturnType(Type returnType)
    {
        return new CustomMethodBuilder(_definition with
        {
            ReturnType = returnType
        });
    }

    public IEasyTypeMethodBuilder WithParameterTypes(params Type[] parameterTypes)
    {
        return new CustomMethodBuilder(_definition with
        {
            ParameterTypes = parameterTypes
        });
    }

    public IEasyTypeMethodBuilder ModifyMethod(Action<MethodBuilder> methodModifier)
    {
        return ModifyMethod((method, _) => methodModifier(method));
    }

    public IEasyTypeMethodBuilder ModifyMethod(Action<MethodBuilder, MethodBuilderContext> methodModifier)
    {
        return new CustomMethodBuilder(_definition with
        {
            Modifiers = _definition.Modifiers.Add(methodModifier)
        });
    }

    public IEasyTypeMethodBuilder WithEmitter(Action<ILGenerator> emitter)
    {
        return WithEmitter((il, _) => emitter(il));
    }

    public IEasyTypeMethodBuilder WithEmitter(Action<ILGenerator, MethodBuilderContext> emitter)
    {
        return new CustomMethodBuilder(_definition with
        {
            Emitter = emitter
        });
    }

    internal CustomMethodDefinition ValidateAndGetDefinition()
    {
        if (_definition.Emitter is null)
        {
            throw new ArgumentNullException(
                nameof(WithEmitter),
                "The Emitter is still unset, make sure to call WithEmitter before you return");
        }
        
        return _definition;
    }
}
