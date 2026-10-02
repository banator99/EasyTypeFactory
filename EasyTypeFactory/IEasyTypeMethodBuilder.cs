using System;
using System.Reflection;
using System.Reflection.Emit;

namespace EasyTypeFactory;

public interface IEasyTypeMethodBuilder
{
    IEasyTypeMethodBuilder WithAttributes(MethodAttributes attributes);
    
    IEasyTypeMethodBuilder WithReturnType(Type returnType);
    
    IEasyTypeMethodBuilder WithParameterTypes(params Type[] parameterTypes);
    
    IEasyTypeMethodBuilder ModifyMethod(Action<MethodBuilder> methodModifier);
    
    IEasyTypeMethodBuilder ModifyMethod(Action<MethodBuilder, MethodBuilderContext> methodModifier);

    IEasyTypeMethodBuilder WithEmitter(Action<ILGenerator> emitter);
    
    IEasyTypeMethodBuilder WithEmitter(Action<ILGenerator, MethodBuilderContext> emitter);
}
