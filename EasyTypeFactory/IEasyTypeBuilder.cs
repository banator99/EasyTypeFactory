using System;
using System.Reflection.Emit;

namespace EasyTypeFactory;

public interface IEasyTypeBuilder : IEquatable<EasyTypeBuilder>
{
    IEasyTypeBuilder Defaults();
    
    IEasyTypeBuilder WithModuleBuilder(ModuleBuilder moduleBuilder);

    IEasyTypeBuilder WithoutModuleBuilder();

    IEasyTypeBuilder WithName(string? name, bool discriminate = true);

    IEasyTypeBuilder WithoutName();

    IEasyTypeBuilder WithBaseType(Type baseType);

    IEasyTypeBuilder WithoutBaseType();

    IEasyTypeBuilder AddField(
        string name,
        Type type,
        bool isReadOnly = false,
        bool isRequired = false,
        object? defaultValue = null);

    IEasyTypeBuilder RemoveField(string name);

    IEasyTypeBuilder ClearFields();

    IEasyTypeBuilder AddInterface(Type interfaceType);

    IEasyTypeBuilder RemoveInterface(Type interfaceType);

    IEasyTypeBuilder ClearInterfaces();

    IEasyTypeBuilder WithGetByName(string? methodName = null);

    IEasyTypeBuilder WithoutGetByName();

    IEasyTypeBuilder WithGenericGetByName(string? methodName = null);

    IEasyTypeBuilder WithoutGenericGetByName();

    IEasyTypeBuilder AddGetTypeByName(Type type);

    IEasyTypeBuilder AddGetTypeByName(string name, Type type);

    IEasyTypeBuilder RemoveGetTypeByName(Type type);

    IEasyTypeBuilder ClearGetTypeByNames();

    IEasyTypeBuilder DoAddAllGetTypeByNameMethods();

    IEasyTypeBuilder DoNotAddAllGetTypeByNameMethods();

    IEasyTypeBuilder WithSetByName(string? methodName = null);
    
    IEasyTypeBuilder WithoutSetByName();
    
    IEasyTypeBuilder WithGenericSetByName(string? methodName = null);
    
    IEasyTypeBuilder WithoutGenericSetByName();
    
    IEasyTypeBuilder AddSetTypeByName(Type type);
    
    IEasyTypeBuilder AddSetTypeByName(string name, Type type);
    
    IEasyTypeBuilder RemoveSetTypeByName(Type type);
    
    IEasyTypeBuilder ClearSetTypeByNames();
    
    IEasyTypeBuilder DoAddAllSetTypeByNameMethods();
    
    IEasyTypeBuilder DoNotAddAllSetTypeByNameMethods();
    
    IEasyTypeBuilder WithIndexer(bool setter = true);
    
    IEasyTypeBuilder WithoutIndexer();
    
    IEasyTypeBuilder AddCustomMethod(
        string methodName,
        Func<IEasyTypeMethodBuilder, IEasyTypeMethodBuilder> builder);
    
    IEasyTypeBuilder RemoveCustomMethods(string methodName);

    IEasyTypeBuilder ClearCustomMethods();
    
    IEasyTypeBuilder WithParameterlessConstructor();
    
    IEasyTypeBuilder WithoutParameterlessConstructor();
    
    IEasyTypeBuilder WithMinimalConstructor();
    
    IEasyTypeBuilder WithoutMinimalConstructor();
    
    IEasyTypeBuilder WithFullConstructor();
    
    IEasyTypeBuilder WithoutFullConstructor();
    
    IEasyTypeBuilder WithEquality(bool operators = true);
    
    IEasyTypeBuilder WithoutEquality();

    IEasyTypeBuilder CaseSensitive();

    IEasyTypeBuilder CaseInsensitive();
    
    Type Build();
}
