using System;
using System.Collections.Concurrent;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace EasyTypeFactory.IntegrationTests;

public class ModelEmitterTests
{
    private static ModuleBuilder CreateModuleBuilder(AssemblyBuilderAccess access = AssemblyBuilderAccess.Run)
    {
        var name = new AssemblyName("ModelEmitterTests_" + Guid.NewGuid().ToString("N"));
        var assembly = AssemblyBuilder.DefineDynamicAssembly(name, access);
        return assembly.DefineDynamicModule(name.Name!);
    }

    [Fact]
    public void Build_ConcurrentDiscriminatedNames_ProducesUniqueUsableTypes()
    {
        var module = CreateModuleBuilder();
        var builder = EasyTypeBuilder.Create("ConcurrentType")
            .WithModuleBuilder(module)
            .AddField("Value", typeof(int))
            .WithFullConstructor();
        var types = new ConcurrentBag<Type>();

        Parallel.For(0, 32, _ => types.Add(builder.Build()));

        Assert.Equal(32, types.Count);
        Assert.Equal(
            Enumerable.Range(0, 32).Select(i => $"ConcurrentType<{i}>").OrderBy(name => name),
            types.Select(type => type.FullName!).OrderBy(name => name));
        foreach (var type in types)
        {
            var instance = Activator.CreateInstance(type, [42]);
            Assert.Equal(42, type.GetField("Value")!.GetValue(instance));
        }
    }

    [Fact]
    public void Build_ConcurrentExplicitNames_OnlyOneBuildSucceeds()
    {
        var module = CreateModuleBuilder();
        var builder = EasyTypeBuilder.Create("ExplicitType", discriminate: false)
            .WithModuleBuilder(module)
            .WithParameterlessConstructor();
        var types = new ConcurrentBag<Type>();
        var failures = new ConcurrentBag<Exception>();

        Parallel.For(0, 32, _ =>
        {
            try
            {
                types.Add(builder.Build());
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        });

        var type = Assert.Single(types);
        Assert.NotNull(Activator.CreateInstance(type));
        Assert.Equal(31, failures.Count);
        Assert.All(failures, exception => Assert.IsType<DuplicateNameException>(exception));
    }

    [Fact]
    public void Build_CollectibleModule_IsNotRetainedByNameTracking()
    {
        var references = BuildCollectibleType();

        for (var attempt = 0; attempt < 10 && references.Any(reference => reference.IsAlive); attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        Assert.All(references, reference => Assert.False(reference.IsAlive));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] BuildCollectibleType()
    {
        var module = CreateModuleBuilder(AssemblyBuilderAccess.RunAndCollect);
        var type = EasyTypeBuilder.Create("CollectibleType")
            .WithModuleBuilder(module)
            .AddField("Value", typeof(int))
            .WithFullConstructor()
            .Build();
        var instance = Activator.CreateInstance(type, [42]);

        Assert.True(type.Assembly.IsCollectible);
        Assert.Equal(42, type.GetField("Value")!.GetValue(instance));

        return [new WeakReference(module), new WeakReference(type.Assembly), new WeakReference(type)];
    }
}