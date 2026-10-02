using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using EasyTypeFactory.Definition;
using EasyTypeFactory.Emission;
using EasyTypeFactory.Model;
using EasyTypeFactory.Utils;

namespace EasyTypeFactory;

public sealed class EasyTypeBuilder : IEasyTypeBuilder
{
    public static readonly IEasyTypeBuilder Empty = new EasyTypeBuilder();
    
    public static readonly IEasyTypeBuilder Default = new EasyTypeBuilder().Defaults();

    public static IEasyTypeBuilder Create(string? name = null, bool discriminate = true)
    {
        return name is null 
            ? Empty 
            : Empty.WithName(name, discriminate);
    }
    
    private readonly ModelDefinition _modelDefinition;

    private EasyTypeBuilder()
    {
        _modelDefinition = new ModelDefinition();
    }

    private EasyTypeBuilder(ModelDefinition modelDefinition)
    {
        _modelDefinition = modelDefinition;
    }
    
    public IEasyTypeBuilder Defaults()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            ParameterlessConstructor = true,
            MinimalConstructor = true,
            FullConstructor = true,
            Equality = true,
            EqualityOperators = true
        });
    }

    public IEasyTypeBuilder WithModuleBuilder(ModuleBuilder moduleBuilder)
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            ModuleBuilder = moduleBuilder
        });
    }
    
    public IEasyTypeBuilder WithoutModuleBuilder()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            ModuleBuilder = null
        });
    }

    public IEasyTypeBuilder WithName(string? name, bool discriminate = true)
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            Name = name, 
            DiscriminateName = discriminate
        });
    }

    public IEasyTypeBuilder WithoutName()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            Name = null, 
            DiscriminateName = true
        });
    }
    
    public IEasyTypeBuilder WithBaseType(Type baseType)
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            BaseType = baseType
        });
    }

    public IEasyTypeBuilder WithoutBaseType()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            BaseType = null
        });
    }
    
    public IEasyTypeBuilder AddField(
        string name,
        Type type,
        bool isReadOnly = false,
        bool isRequired = false,
        object? defaultValue = null)
    {
        if (_modelDefinition.Fields.ContainsKey(name))
        {
            throw new ArgumentException("Field already defined", nameof(name));
        }

        var definition = new FieldDefinition(
            type,
            isReadOnly,
            isRequired,
            defaultValue);

        return new EasyTypeBuilder(_modelDefinition with
        {
            Fields = _modelDefinition.Fields.Add(name, definition)
        });
    }

    public IEasyTypeBuilder RemoveField(string name)
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            Fields = _modelDefinition.Fields.Remove(name)
        });
    }

    public IEasyTypeBuilder ClearFields()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            Fields = _modelDefinition.Fields.Clear()
        });
    }

    public IEasyTypeBuilder AddInterface(Type interfaceType)
    {
        if (_modelDefinition.Interfaces.Contains(interfaceType))
        {
            throw new ArgumentException("Interface already defined", nameof(interfaceType));
        }
        
        return new EasyTypeBuilder(_modelDefinition with
        {
            Interfaces = _modelDefinition.Interfaces.Add(interfaceType)
        });
    }
    
    public IEasyTypeBuilder RemoveInterface(Type interfaceType)
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            Interfaces = _modelDefinition.Interfaces.Remove(interfaceType)
        });
    }

    public IEasyTypeBuilder ClearInterfaces()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            Interfaces = _modelDefinition.Interfaces.Clear()
        });
    }

    public IEasyTypeBuilder WithGetByName(string? methodName = null)
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            GetByName = methodName ?? "GetByName"
        });
    }

    public IEasyTypeBuilder WithoutGetByName()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            GetByName = null
        });
    }
    
    public IEasyTypeBuilder WithGenericGetByName(string? methodName = null)
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            GetTByName = methodName ?? "GetByName"
        });
    }

    public IEasyTypeBuilder WithoutGenericGetByName()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            GetTByName = null
        });
    }

    public IEasyTypeBuilder AddGetTypeByName(Type type)
    {
        return AddGetTypeByName($"Get{type.Name}ByName", type);   
    }
    
    public IEasyTypeBuilder AddGetTypeByName(string name, Type type)
    {
        if (_modelDefinition.GetTypeByName.ContainsKey(type))
        {
            throw new ArgumentException("GetTypeByName already added for the given type.", nameof(type));
        }
        
        return new EasyTypeBuilder(_modelDefinition with
        {
            GetTypeByName = _modelDefinition.GetTypeByName.Add(type, name)
        });
    }

    public IEasyTypeBuilder RemoveGetTypeByName(Type type)
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            GetTypeByName = _modelDefinition.GetTypeByName.Remove(type)
        });
    }
    
    public IEasyTypeBuilder ClearGetTypeByNames()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            GetTypeByName = _modelDefinition.GetTypeByName.Clear()
        });
    }

    public IEasyTypeBuilder DoAddAllGetTypeByNameMethods()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            AddAllGetTypeByNameMethods = true
        });
    }
    
    public IEasyTypeBuilder DoNotAddAllGetTypeByNameMethods()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            AddAllGetTypeByNameMethods = false
        });
    }
    
    public IEasyTypeBuilder WithSetByName(string? methodName = null)
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            SetByName = methodName ?? "SetByName"
        });
    }

    public IEasyTypeBuilder WithoutSetByName()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            SetByName = null
        });
    }
    
    public IEasyTypeBuilder WithGenericSetByName(string? methodName = null)
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            SetTByName = methodName ?? "SetByName"
        });
    }

    public IEasyTypeBuilder WithoutGenericSetByName()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            SetTByName = null
        });
    }

    public IEasyTypeBuilder AddSetTypeByName(Type type)
    {
        return AddSetTypeByName($"Set{type.Name}ByName", type);   
    }
    
    public IEasyTypeBuilder AddSetTypeByName(string name, Type type)
    {
        if (_modelDefinition.SetTypeByName.ContainsKey(type))
        {
            throw new ArgumentException("SetTypeByName already added for the given type.", nameof(type));
        }
        
        return new EasyTypeBuilder(_modelDefinition with
        {
            SetTypeByName = _modelDefinition.SetTypeByName.Add(type, name)
        });
    }

    public IEasyTypeBuilder RemoveSetTypeByName(Type type)
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            SetTypeByName = _modelDefinition.SetTypeByName.Remove(type)
        });
    }
    
    public IEasyTypeBuilder ClearSetTypeByNames()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            SetTypeByName = _modelDefinition.SetTypeByName.Clear()
        });
    }

    public IEasyTypeBuilder DoAddAllSetTypeByNameMethods()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            AddAllSetTypeByNameMethods = true
        });
    }
    
    public IEasyTypeBuilder DoNotAddAllSetTypeByNameMethods()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            AddAllSetTypeByNameMethods = false
        });
    }

    public IEasyTypeBuilder WithIndexer(bool setter = true)
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            IndexerGet = true,
            IndexerSet = setter
        });
    }
    
    public IEasyTypeBuilder WithoutIndexer()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            IndexerGet = false,
            IndexerSet = false
        });
    }

    public IEasyTypeBuilder AddCustomMethod(
        string methodName,
        Func<IEasyTypeMethodBuilder, IEasyTypeMethodBuilder> builder)
    {
        IEasyTypeMethodBuilder methodBuilder = new CustomMethodBuilder(methodName);
        methodBuilder = builder(methodBuilder);

        if (methodBuilder is not CustomMethodBuilder customMethodBuilder)
        {
            throw new InvalidOperationException(
                "The builder returned an unexpected instance. Do not return your own implementation of " +
                "IDynamicModelMethodBuilder.");
        }

        var methodDefinition = customMethodBuilder.ValidateAndGetDefinition();
        
        return new EasyTypeBuilder(_modelDefinition with
        {
            CustomMethods = _modelDefinition.CustomMethods.Add(methodDefinition)
        });
    }

    public IEasyTypeBuilder RemoveCustomMethods(string methodName)
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            CustomMethods = _modelDefinition.CustomMethods.RemoveAll(m => 
                _modelDefinition.CaseInsensitive 
                    ? m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase)
                    : m.Name == methodName)
        });
    }
    
    public IEasyTypeBuilder ClearCustomMethods()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            CustomMethods = []
        });
    }

    public IEasyTypeBuilder WithParameterlessConstructor()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            ParameterlessConstructor = true
        });
    }

    public IEasyTypeBuilder WithoutParameterlessConstructor()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            ParameterlessConstructor = false
        });
    }
    
    public IEasyTypeBuilder WithMinimalConstructor()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            MinimalConstructor = true
        });
    }

    public IEasyTypeBuilder WithoutMinimalConstructor()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            MinimalConstructor = false
        });
    }
    
    public IEasyTypeBuilder WithFullConstructor()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            FullConstructor = true
        });
    }

    public IEasyTypeBuilder WithoutFullConstructor()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            FullConstructor = false
        });
    }
    
    public IEasyTypeBuilder WithEquality(bool operators = true)
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            Equality = true,
            EqualityOperators = operators
        });
    }
    
    public IEasyTypeBuilder WithoutEquality()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            Equality = false,
            EqualityOperators = false
        });
    }
    
    public IEasyTypeBuilder CaseSensitive()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            CaseInsensitive = false,
            Fields = _modelDefinition.Fields.WithComparers(null),
            GetTypeByName = _modelDefinition.GetTypeByName.WithComparers(null, null),
            SetTypeByName = _modelDefinition.SetTypeByName.WithComparers(null, null)
        });
    }

    public IEasyTypeBuilder CaseInsensitive()
    {
        return new EasyTypeBuilder(_modelDefinition with
        {
            CaseInsensitive = true,
            Fields = _modelDefinition.Fields.WithComparers(StringComparer.OrdinalIgnoreCase),
            GetTypeByName = _modelDefinition.GetTypeByName.WithComparers(null, StringComparer.OrdinalIgnoreCase),
            SetTypeByName = _modelDefinition.SetTypeByName.WithComparers(null, StringComparer.OrdinalIgnoreCase)
        });
    }

    public Type Build()
    {
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new PlatformNotSupportedException(
                "This library requires dynamic code generation.");
        }
        
        var finalModel = ModelBuilder.Create(_modelDefinition);
        var moduleBuilder = _modelDefinition.ModuleBuilder ?? Shared.ModuleBuilder;
        var factory = new ModelEmitter(finalModel, moduleBuilder);
        return factory.Create();
    }

    public bool Equals(EasyTypeBuilder? other)
    {
        return other is not null && (ReferenceEquals(this, other) || _modelDefinition.Equals(other._modelDefinition));
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as EasyTypeBuilder);
    }

    public override int GetHashCode()
    {
        return _modelDefinition.GetHashCode();
    }

    public static bool operator ==(EasyTypeBuilder? left, EasyTypeBuilder? right)
    {
        return left is null && right is null || left is not null && left.Equals(right);
    }

    public static bool operator !=(EasyTypeBuilder? left, EasyTypeBuilder? right)
    {
        return !(left == right);
    }
}
