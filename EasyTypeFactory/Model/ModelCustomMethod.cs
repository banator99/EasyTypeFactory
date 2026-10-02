using System;
using System.Reflection;
using System.Reflection.Emit;

namespace EasyTypeFactory.Model;

internal class ModelCustomMethod : ModelMethod
{
    public required MethodAttributes Attributes { get;init; }
    
    public required Action<MethodBuilder, MethodBuilderContext>[] MethodModifiers { get; init; }
    
    public required Action<ILGenerator, MethodBuilderContext> Emitter { get; init; }
}
