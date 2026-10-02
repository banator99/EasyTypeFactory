using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using EasyTypeFactory.Definition;

namespace EasyTypeFactory.Model;

internal sealed class ModelBuilder
{
    private readonly ModelDefinition _modelDefinition;
    
    // We use HashSets to avoid duplicate members. When scanning the interfaces, we may see that multiple interfaces
    // extend the same parent interface. This can result in duplicate members which we mitigate by using a HashSet.
    private readonly Dictionary<string, HashSet<PropertyInfo>> _interfaceProperties = [];
    private readonly Dictionary<string, HashSet<MethodInfo>> _interfaceMethods = [];
    private readonly Dictionary<string, PropertyInfo> _baseClassProperties = [];
    private readonly Dictionary<string, HashSet<MethodInfo>> _baseClassMethods = [];
    private PropertyInfo? _baseClassIndexer;

    private readonly List<ModelField> _fields = [];
    private readonly List<ModelProperty> _properties = [];
    private readonly List<ModelConstructor> _constructors = [];
    private readonly List<ModelMethod> _methods = [];
    private readonly List<ModelOperator> _operators = [];
    private ModelIndexer? _indexer;
    
    private ModelBuilder(ModelDefinition modelDefinition)
    {
        _modelDefinition = modelDefinition;
    }
    
    public static Model Create(ModelDefinition modelDefinition)
    {
        var builder = new ModelBuilder(modelDefinition);
        return builder.Build();
    }

    private Model Build()
    {
        ScanBaseClass();
        ScanInterfaces();
        MapFields();
        MapIndexer();
        MapConstructors();
        MapMethods();
        MapOperators();
        return CreateFinalModel();
    }
    
    private void ScanBaseClass()
    {
        if (_modelDefinition.BaseType is null)
        {
            return;
        }

        if (_modelDefinition.BaseType.IsInterface)
        {
            Scan(_modelDefinition.BaseType, _interfaceProperties, _interfaceMethods, out _);
        }
        else
        {
            var properties = new Dictionary<string, HashSet<PropertyInfo>>();
            
            Scan(
                _modelDefinition.BaseType,
                properties,
                _baseClassMethods,
                out _baseClassIndexer,
                static property => 
                    property.GetGetMethod()?.IsAbstract == true || property.GetSetMethod()?.IsAbstract == true,
                static method => method.IsAbstract);
            
            _baseClassProperties.EnsureCapacity(properties.Count);
            
            foreach (var (propertyName, propertyInfos) in properties)
            {
                _baseClassProperties.Add(propertyName, propertyInfos.Single());
            }
        }
    }
    
    private void ScanInterfaces()
    {
        foreach (var interfaceType in _modelDefinition.Interfaces)
        {
            Scan(interfaceType, _interfaceProperties, _interfaceMethods, out _);
            foreach (var parentInterface in interfaceType.GetInterfaces())
            {
                Scan(parentInterface, _interfaceProperties, _interfaceMethods, out _);
            }
        }
    }
    
    private static void Scan(
        Type type,
        Dictionary<string, HashSet<PropertyInfo>> properties,
        Dictionary<string, HashSet<MethodInfo>> methods,
        out PropertyInfo? indexer,
        Func<PropertyInfo, bool>? propertyFilter = null,
        Func<MethodInfo, bool>? methodFilter = null)
    {
        indexer = null;
        
        var propertyInfos = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var propertyInfo in propertyInfos)
        {
            if (propertyFilter is not null && !propertyFilter(propertyInfo))
            {
                continue;
            }

            if (propertyInfo is
                {
                    IsSpecialName: true,
                    Name: "Item",
                    CanRead: true
                } &&
                propertyInfo.PropertyType == typeof(object))
            {
                var parameters = propertyInfo.GetIndexParameters();
                
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                {
                    indexer = propertyInfo;
                    continue;
                }
            }
            
            if (properties.TryGetValue(propertyInfo.Name, out var propertyInfoSet))
            {
                if (propertyInfoSet.Any(p => p.PropertyType != propertyInfo.PropertyType))
                {
                    throw new InvalidOperationException(
                        $"Types defer for member '{propertyInfo.Name}'. Current type: " +
                        $"'{propertyInfoSet.First().PropertyType}', new type: '{propertyInfo.PropertyType}'.");
                }
                
                propertyInfoSet.Add(propertyInfo);
            }
            else
            {
                properties.Add(propertyInfo.Name, [propertyInfo]);
            }
        }

        foreach (var methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            if (methodFilter is not null && !methodFilter(methodInfo))
            {
                continue;
            }

            if (methodInfo.IsSpecialName)
            {
                continue;
            }
            
            if (methods.TryGetValue(methodInfo.Name, out var methodInfoSet))
            {
                methodInfoSet.Add(methodInfo);
            }
            else
            {
                methods.Add(methodInfo.Name, [methodInfo]);
            }
        }
    }

    private void MapFields()
    {
        foreach (var (fieldName, fieldDefinition) in _modelDefinition.Fields)
        {
            ModelField field;
            bool createBackingField;
            
            PropertyInfo[]? baseClassProperties = null;
            var publicProperties = new HashSet<string>();

            if (_modelDefinition.CaseInsensitive)
            {
                // Create public properties for all of these properties
                
                baseClassProperties = _baseClassProperties
                    .Where(kvp => kvp.Key.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                    .Select(kvp => kvp.Value)
                    .ToArray();
                
                createBackingField = baseClassProperties.Length > 0 || 
                    _interfaceProperties.Keys.Any(k => k.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                if (_baseClassProperties.TryGetValue(fieldName, out var baseProp))
                {
                    baseClassProperties = [baseProp];
                }

                createBackingField = baseClassProperties is not null || _interfaceProperties.ContainsKey(fieldName);
            }
            
            if (createBackingField)
            {
                // Create public property with a backing field
                
                field = new ModelField
                {
                    Name = $"<{fieldName}>k__BackingField",
                    OriginalName = fieldName,
                    Type = fieldDefinition.Type,
                    DefaultValue = fieldDefinition.DefaultValue,
                    ReadOnly = fieldDefinition.ReadOnly,
                    Required = fieldDefinition.Required,
                    Private = true
                };

                _fields.Add(field);
                
                if (baseClassProperties is not null)
                {
                    foreach (var baseClassProperty in baseClassProperties)
                    {
                        var property = new ModelProperty
                        {
                            Name = baseClassProperty.Name,
                            Field = field,
                            HasInit = fieldDefinition.ReadOnly,
                            HasSet = !fieldDefinition.ReadOnly,
                            OverriddenGetMethod = baseClassProperty.GetGetMethod(),
                            OverriddenSetMethod = baseClassProperty.GetSetMethod()
                        };
                
                        _properties.Add(property);
                        publicProperties.Add(baseClassProperty.Name);
                    }
                }
            }
            else
            {
                // Create public field
                
                field = new ModelField
                {
                    Name = fieldName,
                    OriginalName = fieldName,
                    Type = fieldDefinition.Type,
                    DefaultValue = fieldDefinition.DefaultValue,
                    ReadOnly = fieldDefinition.ReadOnly,
                    Required = fieldDefinition.Required
                };
                
                _fields.Add(field);
            }

            var propertyInfos = _modelDefinition.CaseInsensitive
                ? _interfaceProperties
                    .Where(kvp => kvp.Key.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                    .SelectMany(kvp => kvp.Value)
                : _interfaceProperties
                    .Where(kvp => kvp.Key == fieldName)
                    .SelectMany(kvp => kvp.Value);
        
            foreach (var propertyInfo in propertyInfos)
            {
                if (publicProperties.Contains(propertyInfo.Name))
                {
                    // A public property with the same name already exists, do not add the property again
                    continue;
                }
                
                Type? explicitImplementationType = null;
                MethodInfo? overridenGetMethod = null;
                MethodInfo? overridenSetMethod = null;

                if (propertyInfo.Name == fieldName)
                {
                    // If the name of the property equals to the name of the field, make it a public property
                    publicProperties.Add(fieldName);
                }
                else
                {
                    // If the name of the property has different casing, make it an explicit property implementation
                    explicitImplementationType = propertyInfo.DeclaringType;
                    
                    overridenGetMethod = propertyInfo.GetGetMethod();
                    overridenSetMethod = propertyInfo.GetSetMethod();
                }
                
                var property = new ModelProperty
                {
                    Name = fieldName,
                    Field = field,
                    HasInit = fieldDefinition.ReadOnly,
                    HasSet = !fieldDefinition.ReadOnly,
                    ExplicitImplementationType = explicitImplementationType,
                    OverriddenGetMethod = overridenGetMethod,
                    OverriddenSetMethod = overridenSetMethod
                };
                
                _properties.Add(property);
            }
        }
    }

    private void MapIndexer()
    {
        if (!_modelDefinition.IndexerGet)
        {
            return;
        }
        
        _indexer = new ModelIndexer
        {
            HasSet = _modelDefinition.IndexerSet,
            OverriddenGetMethod = _baseClassIndexer?.GetGetMethod(),
            OverriddenSetMethod = _baseClassIndexer?.GetSetMethod()
        };
    }
    
    private void MapConstructors()
    {
        var minimal = _fields
            .Where(field => field.ReadOnly || field.Required || field.DefaultValue is null)
            .ToArray();
        
        var createParameterless = _modelDefinition.ParameterlessConstructor || 
            (_fields.All(field => !field.ReadOnly) && !_modelDefinition.MinimalConstructor && !_modelDefinition.FullConstructor);
        
        var createMinimal = _modelDefinition.MinimalConstructor && minimal.Length > 0 && minimal.Length < _fields.Count;
        
        var createFull = _modelDefinition.FullConstructor || (!createParameterless && !createMinimal);

        if (createParameterless)
        {
            _constructors.Add(new ModelConstructor
            {
                Fields = []
            });
        }

        if (createMinimal)
        {
            _constructors.Add(new ModelConstructor
            {
                Fields = minimal,
                SetsRequiredMembers = minimal.Any(field => field.Required)
            });
        }

        if (createFull)
        {
            _constructors.Add(new ModelConstructor
            {
                Fields = _fields.ToArray(),
                SetsRequiredMembers = _fields.Any(field => field.Required)
            });
        }
    }

    private void MapMethods()
    {
        ModelMethod method;
        
        if (_modelDefinition.GetByName is not null)
        {
            method = new ModelGetByNameMethod
            {
                Name = _modelDefinition.GetByName
            };

            method.OverriddenMethod = GetBaseMethod(method);
            
            _methods.Add(method);
        }

        if (_modelDefinition.GetTByName is not null)
        {
            method = new ModelGetTByNameMethod
            {
                Name = _modelDefinition.GetTByName
            };

            method.OverriddenMethod = GetBaseMethod(method);
            
            _methods.Add(method);
        }

        var getTypeByName = new Dictionary<Type, string>(_modelDefinition.GetTypeByName);
        if (_modelDefinition.AddAllGetTypeByNameMethods)
        {
            foreach (var fieldType in _fields.Select(f => f.Type).Distinct())
            {
                getTypeByName.TryAdd(fieldType, $"Get{fieldType.Name}ByName");
            }
        }

        foreach (var (type, methodName) in getTypeByName)
        {
            method = new ModelGetTypeByNameMethod
            {
                Name = methodName,
                ReturnType = type
            };

            method.OverriddenMethod = GetBaseMethod(method);
            
            _methods.Add(method);
        }
        
        if (_modelDefinition.SetByName is not null)
        {
            method = new ModelSetByNameMethod
            {
                Name = _modelDefinition.SetByName
            };

            method.OverriddenMethod = GetBaseMethod(method);
            
            _methods.Add(method);
        }

        if (_modelDefinition.SetTByName is not null)
        {
            method = new ModelSetTByNameMethod
            {
                Name = _modelDefinition.SetTByName
            };

            method.OverriddenMethod = GetBaseMethod(method);
            
            _methods.Add(method);
        }

        var setTypeByName = new Dictionary<Type, string>(_modelDefinition.SetTypeByName);
        if (_modelDefinition.AddAllSetTypeByNameMethods)
        {
            foreach (var fieldType in _fields.Select(f => f.Type).Distinct())
            {
                setTypeByName.TryAdd(fieldType, $"Set{fieldType.Name}ByName");
            }
        }

        foreach (var (type, methodName) in setTypeByName)
        {
            method = new ModelSetTypeByNameMethod
            {
                Name = methodName,
                Parameters = [typeof(string), type]
            };

            method.OverriddenMethod = GetBaseMethod(method);
            
            _methods.Add(method);
        }

        foreach (var customMethod in _modelDefinition.CustomMethods)
        {
            Debug.Assert(
                customMethod.Emitter is not null,
                "The Emitter of a custom method must not be null, make sure to validate this before " +
                "providing model definition to the FinalModelBuilder.");
            
            method = new ModelCustomMethod
            {
                Name = customMethod.Name,
                Attributes = customMethod.Attributes,
                ReturnType = customMethod.ReturnType ?? typeof(void),
                Parameters = customMethod.ParameterTypes,
                MethodModifiers = customMethod.Modifiers.ToArray(),
                Emitter = customMethod.Emitter
            };
            
            _methods.Add(method);
        }

        if (!_modelDefinition.Equality)
        {
            return;
        }

        _methods.Add(new ModelEqualsTypeMethod());
        _methods.Add(new ModelEqualsObjectMethod());
        _methods.Add(new ModelGetHashCodeMethod());
    }

    private MethodInfo? GetBaseMethod(ModelMethod method)
    {
        if (!_baseClassMethods.TryGetValue(method.Name, out var methodInfos))
        {
            return null;
        }
        
        Debug.Assert(method.Parameters is not null, "This method must only be called for methods with known parameters");
        
        return methodInfos.SingleOrDefault(methodInfo =>
            methodInfo.ReturnType == method.ReturnType &&
            methodInfo
                .GetParameters()
                .Select(parameter => parameter.ParameterType)
                .SequenceEqual(method.Parameters));
    }

    private void MapOperators()
    {
        if (!_modelDefinition.EqualityOperators)
        {
            return;
        }
        
        _operators.Add(new ModelEqualityOperator());
        _operators.Add(new ModelInequalityOperator());
    }

    private Model CreateFinalModel()
    {
        IEnumerable<Type> interfaces = _modelDefinition.Interfaces;
        var baseType = _modelDefinition.BaseType;
        
        if (baseType?.IsInterface == true)
        {
            interfaces = interfaces.Append(baseType);
            baseType = null;
        }

        if (_modelDefinition.Equality)
        {
            interfaces = interfaces.Append(typeof(IEquatable<>));
        }
        
        return new Model
        {
            Name = _modelDefinition.Name,
            DiscriminateName = _modelDefinition.DiscriminateName,
            Fields = [.._fields],
            Properties = [.._properties],
            Indexer = _indexer,
            Constructors = [.._constructors],
            Methods = [.._methods],
            Operators = [.._operators],
            Interfaces = [..interfaces],
            BaseType = baseType,
            CaseInsensitive = _modelDefinition.CaseInsensitive
        };
    }
}
