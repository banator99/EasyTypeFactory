using System;
using System.Reflection;
using System.Reflection.Emit;

namespace EasyTypeFactory.Utils;

internal static class Shared
{
    public const string DefaultNamespace = "EasyTypeFactory.Generated";
    
    private const string DefaultAssemblyName = DefaultNamespace;
    
    private const string DefaultModuleName = DefaultNamespace;
    
    public const string DefaultClassName = "Anonymous";
    
    public const string DefaultFullName = DefaultNamespace + "." + DefaultClassName;
    
    private static readonly Lazy<ModuleBuilder> ModuleBuilderLazy = new(() =>
    {
        var assemblyName = new AssemblyName(DefaultAssemblyName);
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        return assemblyBuilder.DefineDynamicModule(DefaultModuleName);
    });
    
    public static ModuleBuilder ModuleBuilder => ModuleBuilderLazy.Value;
}
