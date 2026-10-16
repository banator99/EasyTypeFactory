using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using EasyTypeFactory.Model;
using EasyTypeFactory.Utils;

namespace EasyTypeFactory.Emission;

internal sealed class ModelEmitter
{
    // private static readonly ConcurrentDictionary<ModuleBuilder, ConcurrentDictionary<string, uint>> Discriminators = new();
    // private static readonly ConcurrentDictionary<ModuleBuilder, ConcurrentBag<string>> DefinedTypeNames = new();

    private static readonly ConditionalWeakTable<ModuleBuilder, ModuleState> ModuleStates = [];
    
    private readonly Model.Model _model;
    private readonly ModuleBuilder _moduleBuilder;
    private readonly Dictionary<ModelField, FieldBuilder> _fields = [];
    private TypeBuilder _typeBuilder;
    private MethodBuilder? _equalsTypeMethod;
    private MethodBuilder? _opEqualityMethod;
    
    public ModelEmitter(Model.Model model, ModuleBuilder moduleBuilder)
    {
        _model = model;
        _moduleBuilder = moduleBuilder;
        _typeBuilder = null!;
    }

    public Type Create()
    {
        CreateTypeBuilder();
        
        DefineFields();
        EmitProperties();
        EmitIndexer();
        EmitConstructors();
        EmitMethods();
        EmitOperators();
        
        return _typeBuilder.CreateType();
    }
    
    private void CreateTypeBuilder()
    {
        var fullName = _model.Name ?? Shared.DefaultFullName;
        var discriminate = _model.Name is null || _model.DiscriminateName;

        var state = ModuleStates.GetValue(
            _moduleBuilder,
            static _ => new ModuleState());

        if (discriminate)
        {
            while (true)
            {
                var discriminator = state.Discriminators.AddOrUpdate(
                    fullName,
                    0,
                    static (_, current) => checked(current + 1));

                var candidate = $"{fullName}<{discriminator}>";

                if (!state.DefinedTypeNames.TryAdd(candidate, 0))
                {
                    continue;
                }
                
                fullName = candidate;
                break;
            }
        }
        else if (!state.DefinedTypeNames.TryAdd(fullName, 0))
        {
            throw new DuplicateNameException(
                "There is already a type present with the name: " + fullName);
        }

        _typeBuilder = _moduleBuilder.DefineType(
            fullName,
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class,
            _model.BaseType);

        foreach (var interfaceType in _model.Interfaces)
        {
            _typeBuilder.AddInterfaceImplementation(
                interfaceType == typeof(IEquatable<>)
                    ? interfaceType.MakeGenericType(_typeBuilder)
                    : interfaceType);
        }
    }

    private void DefineFields()
    {
        foreach (var field in _model.Fields)
        {
            var attributes = field.Private 
                ? FieldAttributes.Private 
                : FieldAttributes.Public;

            if (field.ReadOnly)
            {
                attributes |= FieldAttributes.InitOnly;
            }

            var fieldBuilder = _typeBuilder.DefineField(
                field.Name,
                field.Type,
                attributes);

            if (field.Required)
            {
                fieldBuilder.SetCustomAttribute(Utils.RequiredMemberAttribute);
            }
            
            _fields.Add(field, fieldBuilder);
        }
    }

    private void EmitProperties()
    {
        foreach (var property in _model.Properties)
        {
            var emitter = new PropertyEmitter(_typeBuilder, property, _fields[property.Field]);
            emitter.Emit();
        }
    }

    private void EmitIndexer()
    {
        if (_model.Indexer is not null)
        {
            var emitter = new IndexerEmitter(_model.Indexer, _typeBuilder, _fields, _model.CaseInsensitive);
            emitter.Emit();
        }
    }

    private void EmitConstructors()
    {
        foreach (var constructor in _model.Constructors)
        {
            var emitter = new ConstructorEmitter(_typeBuilder, constructor, _fields);
            emitter.Emit();
        }
    }

    private void EmitMethods()
    {
        foreach (var method in _model.Methods)
        {
            if (method is ModelEqualsTypeMethod)
            {
                var equalsEmitter = new EqualsTypeEmitter(_typeBuilder, _fields);
                equalsEmitter.Emit();
                _equalsTypeMethod = equalsEmitter.Method;
            }
            else
            {
                IEmitter emitter = method switch
                {
                    ModelGetByNameMethod getByNameMethod => 
                        new GetByNameEmitter(getByNameMethod, _typeBuilder, _fields, _model.CaseInsensitive),
                    
                    ModelGetTByNameMethod getTByNameMethod => 
                        new GetTByNameEmitter(getTByNameMethod, _typeBuilder, _fields, _model.CaseInsensitive),
                    
                    ModelGetTypeByNameMethod getTypeByNameMethod => 
                        new GetTypeByNameEmitter(getTypeByNameMethod, _typeBuilder, _fields, _model.CaseInsensitive),
                    
                    ModelSetByNameMethod setByNameMethod => 
                        new SetByNameEmitter(setByNameMethod, _typeBuilder, _fields, _model.CaseInsensitive),
                    
                    ModelSetTByNameMethod setTByNameMethod => 
                        new SetTByNameEmitter(setTByNameMethod, _typeBuilder, _fields, _model.CaseInsensitive),
                    
                    ModelSetTypeByNameMethod setTypeByNameMethod => 
                        new SetTypeByNameEmitter(setTypeByNameMethod, _typeBuilder, _fields, _model.CaseInsensitive),
                    
                    ModelEqualsObjectMethod => new EqualsObjectEmitter(_typeBuilder, _fields),
                    ModelGetHashCodeMethod => new GetHashCodeEmitter(_typeBuilder, _fields),
                    ModelCustomMethod customMethod => new CustomMethodEmitter(_typeBuilder, customMethod),
                    
                    _ => throw new NotSupportedException(
                        $"Emission for method '{method.GetType().Name}' is not implemented.")
                };
                
                emitter.Emit();
            }
        }
    }

    private void EmitOperators()
    {
        foreach (var @operator in _model.Operators)
        {
            switch (@operator)
            {
                case ModelEqualityOperator:
                {
                    var emitter = new OperatorEqualityEmitter(_typeBuilder, _equalsTypeMethod!);
                    emitter.Emit();
                    _opEqualityMethod = emitter.Method;
                    break;
                }
                
                case ModelInequalityOperator:
                {
                    var emitter = new OperatorInequalityEmitter(_typeBuilder, _opEqualityMethod!);
                    emitter.Emit();
                    break;
                }
                
                default:
                    throw new NotSupportedException(
                        $"Emission for operator '{@operator.GetType().Name}' is not implemented.");
            }
        }
    }

    private sealed class ModuleState
    {
        public readonly ConcurrentDictionary<string, uint> Discriminators = new();

        public readonly ConcurrentDictionary<string, byte> DefinedTypeNames = new();
    }
}
