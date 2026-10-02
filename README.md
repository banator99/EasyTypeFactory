# EasyTypeFactory

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Target: .NET 8.0](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![Target: .NET 9.0](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)
[![Target: .NET 10.0](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)

**EasyTypeFactory** is an efficient, high-performance, fluent dynamic type generator for .NET. It streamlines the creation of runtime types, removing the friction and boilerplate of manual `System.Reflection.Emit` and MSIL generation.

By providing switch-based runtime member dispatch, automatic constructor synthesis, full `IEquatable<T>` generation, interface implementation, abstract base class inheritance, and custom method emission (via IL or LINQ Expression trees), EasyTypeFactory maximizes runtime throughput and developer productivity.

---

## Table of Contents

- [Why EasyTypeFactory?](#why-easytypefactory)
- [Projects in this Repository](#projects-in-this-repository)
- [NuGet Packages](#nuget-packages)
  - [Package Installation](#package-installation)
- [Feature Overview](#feature-overview)
- [How-To Guides](#how-to-guides)
  - [1. Easy: Getting Started](#1-easy-getting-started)
    - [Basic Dynamic Type](#basic-dynamic-type)
    - [Fields with Default Values and Constraints](#fields-with-default-values-and-constraints)
    - [Using Defaults() for Instant Usability](#using-defaults-for-instant-usability)
  - [2. Intermediate: Data Access & Operations](#2-intermediate-data-access--operations)
    - [Fine-Grained Constructor Management](#fine-grained-constructor-management)
    - [Name-Based Property Access (GetByName & SetByName)](#name-based-property-access-getbyname--setbyname)
    - [Strongly Typed and Specialized Accessors](#strongly-typed-and-specialized-accessors)
    - [String Indexer (`this[name]`)](#string-indexer-thisname)
    - [Case Sensitivity Configuration](#case-sensitivity-configuration)
    - [Structural Equality and Comparison Operators](#structural-equality-and-comparison-operators)
  - [3. Advanced: Architecture & Extensibility](#3-advanced-architecture--extensibility)
    - [Implementing Interfaces Dynamically](#implementing-interfaces-dynamically)
    - [Inheriting from Base Classes](#inheriting-from-base-classes)
    - [Emitting Custom Methods with Raw IL](#emitting-custom-methods-with-raw-il)
    - [Expression-Based Methods (Hyperbee Integration)](#expression-based-methods-hyperbee-integration)
    - [Custom Dynamic Assembly & Module Management](#custom-dynamic-assembly--module-management)
    - [Builder Immutability & Reusable Type Templates](#builder-immutability--reusable-type-templates)
- [API Quick Reference](#api-quick-reference)
- [Performance & Design Notes](#performance--design-notes)
- [License](#license)

---

## Why EasyTypeFactory?

When applications require dynamic schema generation, data transformation proxies, dynamic DTOs, or runtime compilation, developers often face trade-offs between slow reflection-based dynamic objects (`ExpandoObject`, dictionaries) and complex, error-prone `Reflection.Emit` code.

EasyTypeFactory delivers practical utility by eliminating this compromise:
- **Maximum Execution Speed**: Emits compiled CIL byte code directly into dynamic assemblies.
- **Zero-Reflection Access**: Emits jump tables (efficient switch statements) for name-based lookup and indexers rather than relying on slow runtime reflection calls.
- **Immutable Builder API**: Builders are thread-safe and reusable, allowing base templates to branch into different types safely.
- **Full Type System Fidelity**: Generated types are real .NET types with interfaces, inheritance, constructors, attributes, and operators.

---

## Projects in this Repository

| Project | Description |
| :--- | :--- |
| **`EasyTypeFactory`** | The core library containing the fluent builder (`EasyTypeBuilder`), immutable models, and MSIL code emitters for fields, constructors, indexers, getters, setters, and equality. |
| **`EasyTypeFactory.HyperbeeExpressionCompiler`** | An extension library providing LINQ `Expression` tree method compilation on top of `Hyperbee.Expressions.Compiler`, avoiding manual IL coding for complex custom methods. |
| **`EasyTypeFactory.UnitTests`** | Unit tests targeting builder state transitions, equality comparisons, definitions, and individual IL emitter components. |
| **`EasyTypeFactory.IntegrationTests`** | Comprehensive end-to-end integration test suite verifying runtime execution, constructor invocations, interface implementations, base classes, and performance characteristics. |

---

## NuGet Packages

*(Upcoming releases – packages will be available on NuGet shortly)*

### 1. `EasyTypeFactory`
The core dynamic type builder package. Contains everything needed to generate POCOs, DTOs, dynamic implementations of interfaces, constructors, and IL-based members.

### 2. `EasyTypeFactory.HyperbeeExpressionCompiler`
An optional companion package for applications that want to compile C# LINQ `Expression` trees directly into instance or static methods on dynamic types.

### Package Installation

When available on NuGet, install via the .NET CLI:

```bash
# Core package
dotnet add package EasyTypeFactory

# Optional expression compiler extension
dotnet add package EasyTypeFactory.HyperbeeExpressionCompiler
```

Or via the Package Manager Console in Visual Studio / JetBrains Rider:

```powershell
Install-Package EasyTypeFactory
Install-Package EasyTypeFactory.HyperbeeExpressionCompiler
```

---

## Feature Overview

- **Fluid, Immutable API**: Every `.With*()` or `.Add*()` invocation produces a new builder instance, enabling deterministic branching and template reuse.
- **Rich Constructor Synthesis**:
  - *Parameterless constructor* with automatic default value initialization.
  - *Minimal constructor* requiring only mandatory / non-default fields (emits `[SetsRequiredMembers]`).
  - *Full constructor* taking all declared fields in order.
- **High-Performance Name Dispatch**:
  - `GetByName(string name)` / `SetByName(string name, object value)`
  - Generic `GetByName<T>(string name)` / `SetByName<T>(string name, T value)`
  - Type-specialized methods like `GetInt32ByName(string name)` or `SetStringByName(string name, string value)`
- **Dictionary-style Indexer**: Emits `this[string name]` with optional read-only mode and case-insensitive matching.
- **Complete Equality Generation**: Implements `IEquatable<T>`, overrides `Equals(object)` and `GetHashCode()`, and generates `==` and `!=` static operators.
- **Polymorphism Support**: Implement multiple interfaces (with automatic property mapping) or derive from abstract/concrete base classes.
- **Custom Methods**: Low-level `ILGenerator` emission or high-level `LambdaExpression` compilation.

---

## How-To Guides

### 1. Easy: Getting Started

#### Basic Dynamic Type

Generate a new dynamic type with fields in just a few lines of code:

```csharp
using EasyTypeFactory;

// 1. Define the type fluently
Type customerType = EasyTypeBuilder.Create("MyApp.Models.Customer", discriminate: false)
    .AddField("Id", typeof(int))
    .AddField("Name", typeof(string))
    .WithParameterlessConstructor()
    .Build();

// 2. Instantiate and use via standard reflection or dynamic
object customer = Activator.CreateInstance(customerType)!;

customerType.GetField("Id")!.SetValue(customer, 101);
customerType.GetField("Name")!.SetValue(customer, "Alice Smith");

Console.WriteLine($"Created {customerType.FullName}");
```

#### Fields with Default Values and Constraints

Define default values, read-only (`initonly`) fields, and required members:

```csharp
Type orderType = EasyTypeBuilder.Create("MyApp.Models.Order")
    .AddField("OrderId", typeof(Guid), isReadOnly: true)
    .AddField("CustomerEmail", typeof(string), isRequired: true)
    .AddField("Status", typeof(string), defaultValue: "Pending")
    .AddField("TotalAmount", typeof(decimal), defaultValue: 0.0m)
    .WithParameterlessConstructor()
    .Build();

object order = Activator.CreateInstance(orderType)!;
Console.WriteLine(orderType.GetField("Status")!.GetValue(order)); // Output: Pending
```

#### Using Defaults() for Instant Usability

Calling `.Defaults()` applies standard best practices in one call: it configures parameterless, minimal, and full constructors, along with complete `IEquatable<T>` equality and comparison operators.

```csharp
Type productType = EasyTypeBuilder.Create("MyApp.Models.Product")
    .AddField("Sku", typeof(string), isRequired: true)
    .AddField("Price", typeof(double), defaultValue: 9.99)
    .Defaults()
    .Build();

// Uses full constructor (Sku, Price)
object product1 = Activator.CreateInstance(productType, "SKU-1234", 19.99)!;

// Uses minimal constructor (Sku only; Price gets default value 9.99)
object product2 = Activator.CreateInstance(productType, "SKU-1234")!;

// Uses parameterless constructor
object product3 = Activator.CreateInstance(productType)!;

// Structural equality works out of the box
bool areEqual = product1.Equals(product2);
```

---

### 2. Intermediate: Data Access & Operations

#### Fine-Grained Constructor Management

You can selectively enable or disable specific constructor signatures to maintain precise instantiation contracts:

```csharp
Type userType = EasyTypeBuilder.Create("MyApp.Models.User")
    .AddField("Id", typeof(int), isRequired: true)
    .AddField("Username", typeof(string), isRequired: true)
    .AddField("Role", typeof(string), defaultValue: "StandardUser")
    .WithParameterlessConstructor()       // ()
    .WithMinimalConstructor()             // (int Id, string Username)
    .WithFullConstructor()                // (int Id, string Username, string Role)
    .Build();
```

#### Name-Based Property Access (GetByName & SetByName)

Instead of slow reflection to inspect dynamic objects, configure compiled accessor methods:

```csharp
Type recordType = EasyTypeBuilder.Create("MyApp.Models.DataRecord")
    .AddField("Score", typeof(int), defaultValue: 100)
    .AddField("Label", typeof(string), defaultValue: "Sample")
    .WithParameterlessConstructor()
    .WithGetByName()      // Emits: object? GetByName(string name)
    .WithSetByName()      // Emits: void SetByName(string name, object? value)
    .Build();

dynamic record = Activator.CreateInstance(recordType)!;

// Fast dynamic invocation or reflection method invocation
record.SetByName("Score", 250);
int score = (int)record.GetByName("Score");
Console.WriteLine($"Updated Score: {score}"); // Output: 250
```

#### Strongly Typed and Specialized Accessors

To avoid boxing/unboxing overhead, generate generic or type-specialized accessors:

```csharp
Type entityType = EasyTypeBuilder.Create("MyApp.Models.Entity")
    .AddField("Count", typeof(int), defaultValue: 42)
    .AddField("Tag", typeof(string), defaultValue: "Beta")
    .WithParameterlessConstructor()
    .WithGenericGetByName()                   // Emits: T GetByName<T>(string name)
    .WithGenericSetByName()                   // Emits: void SetByName<T>(string name, T value)
    .AddGetTypeByName(typeof(int))            // Emits: int GetInt32ByName(string name)
    .AddSetTypeByName("UpdateString", typeof(string)) // Custom named setter
    .DoAddAllGetTypeByNameMethods()           // Emits specialized getters for all field types
    .Build();
```

#### String Indexer (`this[name]`)

Expose dictionary-like indexing directly on your compiled dynamic objects:

```csharp
Type dictionaryLikeType = EasyTypeBuilder.Create("MyApp.Models.FlexObject")
    .AddField("Title", typeof(string), defaultValue: "Untitled")
    .AddField("Views", typeof(int), defaultValue: 0)
    .WithParameterlessConstructor()
    .WithIndexer(setter: true) // Emits: object? this[string name] { get; set; }
    .Build();

dynamic instance = Activator.CreateInstance(dictionaryLikeType)!;
instance["Views"] = 500;
Console.WriteLine(instance["Title"]); // Output: Untitled
Console.WriteLine(instance["Views"]); // Output: 500
```

#### Case Sensitivity Configuration

By default, lookups match exact casing. You can switch to case-insensitive field matching for JSON, database mapping, or user input scenarios:

```csharp
Type configType = EasyTypeBuilder.Create("MyApp.Models.Config")
    .AddField("ApiKey", typeof(string), defaultValue: "secret-123")
    .WithParameterlessConstructor()
    .WithIndexer()
    .CaseInsensitive() // Field lookup matches "apikey", "APIKEY", etc.
    .Build();

dynamic config = Activator.CreateInstance(configType)!;
Console.WriteLine(config["apikey"]); // Output: secret-123
```

#### Structural Equality and Comparison Operators

Automatically implement `IEquatable<T>`, override `Object.Equals` and `Object.GetHashCode`, and emit operator overloads (`==`, `!=`):

```csharp
Type pointType = EasyTypeBuilder.Create("MyApp.Models.Point")
    .AddField("X", typeof(int))
    .AddField("Y", typeof(int))
    .WithFullConstructor()
    .WithEquality(operators: true)
    .Build();

dynamic p1 = Activator.CreateInstance(pointType, 10, 20)!;
dynamic p2 = Activator.CreateInstance(pointType, 10, 20)!;
dynamic p3 = Activator.CreateInstance(pointType, 15, 30)!;

Console.WriteLine(p1.Equals(p2)); // true
Console.WriteLine(p1 == p2);      // true
Console.WriteLine(p1 != p3);      // true
Console.WriteLine(p1.GetHashCode() == p2.GetHashCode()); // true
```

---

### 3. Advanced: Architecture & Extensibility

#### Implementing Interfaces Dynamically

Map interfaces directly to dynamic types. EasyTypeFactory automatically creates the necessary property implementations backed by fields:

```csharp
public interface IIdentifiable
{
    int Id { get; set; }
    string Code { get; }
}

Type implType = EasyTypeBuilder.Create("MyApp.Models.IdentifiableImpl")
    .AddInterface(typeof(IIdentifiable))
    .AddField("Id", typeof(int))
    .AddField("Code", typeof(string), isReadOnly: true)
    .WithFullConstructor()
    .Build();

IIdentifiable instance = (IIdentifiable)Activator.CreateInstance(implType, 42, "CODE-99")!;
Console.WriteLine($"Id: {instance.Id}, Code: {instance.Code}");

instance.Id = 100; // Modifies backing field
```

#### Inheriting from Base Classes

Inherit from existing abstract or concrete classes and implement abstract properties cleanly:

```csharp
public abstract class EntityBase
{
    public abstract int Id { get; set; }
    public abstract string TenantId { get; }
    
    public void PrintInfo() => Console.WriteLine($"Entity {Id} ({TenantId})");
}

Type derivedType = EasyTypeBuilder.Create("MyApp.Models.ConcreteEntity")
    .WithBaseType(typeof(EntityBase))
    .AddField("Id", typeof(int))
    .AddField("TenantId", typeof(string), isReadOnly: true)
    .WithFullConstructor()
    .Build();

EntityBase entity = (EntityBase)Activator.CreateInstance(derivedType, 1, "US-EAST")!;
entity.PrintInfo(); // Output: Entity 1 (US-EAST)
```

#### Emitting Custom Methods with Raw IL

Attach custom member functions directly using `ILGenerator`:

```csharp
using System.Reflection;
using System.Reflection.Emit;

Type mathType = EasyTypeBuilder.Create("MyApp.Models.Calculator")
    .WithParameterlessConstructor()
    .AddCustomMethod("Multiply", method => method
        .WithAttributes(MethodAttributes.Public | MethodAttributes.HideBySig)
        .WithReturnType(typeof(int))
        .WithParameterTypes(typeof(int), typeof(int))
        .WithEmitter((il, context) =>
        {
            il.Emit(OpCodes.Ldarg_1); // arg 1: first int
            il.Emit(OpCodes.Ldarg_2); // arg 2: second int
            il.Emit(OpCodes.Mul);     // multiply
            il.Emit(OpCodes.Ret);     // return result
        }))
    .Build();

dynamic calc = Activator.CreateInstance(mathType)!;
int product = calc.Multiply(6, 7);
Console.WriteLine($"Result: {product}"); // Output: 42
```

#### Expression-Based Methods (Hyperbee Integration)

With `EasyTypeFactory.HyperbeeExpressionCompiler`, compile LINQ Expressions into dynamic instance or static methods without writing MSIL:

```csharp
using System.Linq.Expressions;
using EasyTypeFactory;
using EasyTypeFactory.HyperbeeExpressionCompiler;

// Define an expression: (int a, int b) => a * b + 10
Expression<Func<int, int, int>> expr = (a, b) => a * b + 10;

Type compiledType = EasyTypeBuilder.Create("MyApp.Models.ExpressionCalculator")
    .WithParameterlessConstructor()
    .AddMethodFromExpression("Compute", expr)
    .Build();

dynamic calc = Activator.CreateInstance(compiledType)!;
int result = calc.Compute(5, 4);
Console.WriteLine($"Computed: {result}"); // Output: 30
```

#### Custom Dynamic Assembly & Module Management

Persist or isolate generated types into a dedicated `ModuleBuilder`:

```csharp
using System.Reflection;
using System.Reflection.Emit;

var assemblyName = new AssemblyName("MyCustomDynamicModuleAssembly");
var dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
var dynamicModule = dynamicAssembly.DefineDynamicModule("MainModule");

Type customType = EasyTypeBuilder.Create("MyApp.Models.SharedType")
    .WithModuleBuilder(dynamicModule)
    .AddField("Timestamp", typeof(DateTime))
    .WithParameterlessConstructor()
    .Build();

Console.WriteLine($"Assembly: {customType.Assembly.FullName}");
```

#### Builder Immutability & Reusable Type Templates

Because builders are immutable records under the hood, you can define a base template and derive multiple specialized schemas with zero race conditions:

```csharp
// Define a base template
IEasyTypeBuilder entityTemplate = EasyTypeBuilder.Create()
    .AddField("Id", typeof(Guid), isReadOnly: true)
    .AddField("CreatedAt", typeof(DateTime), defaultValue: DateTime.UtcNow)
    .Defaults();

// Specialize into User type
Type userType = entityTemplate
    .WithName("MyApp.Models.User")
    .AddField("Username", typeof(string))
    .Build();

// Specialize into Order type
Type orderType = entityTemplate
    .WithName("MyApp.Models.Order")
    .AddField("Total", typeof(decimal))
    .Build();
```

---

## API Quick Reference

### Type Configuration
- `EasyTypeBuilder.Create(string? name = null, bool discriminate = true)`: Creates an empty builder. If `discriminate` is `true`, unique suffixes are added to avoid name collisions.
- `Defaults()`: Enables parameterless, minimal, and full constructors, equality implementation, and operators.
- `WithModuleBuilder(ModuleBuilder mb)` / `WithoutModuleBuilder()`: Configures target dynamic module.
- `WithBaseType(Type baseType)` / `WithoutBaseType()`: Sets or resets class inheritance.
- `AddInterface(Type interfaceType)` / `RemoveInterface(Type)` / `ClearInterfaces()`: Manages interface implementations.
- `CaseSensitive()` / `CaseInsensitive()`: Sets member name resolution casing.
- `Build()`: Emits and completes the dynamic `System.Type`.

### Field & Property Setup
- `AddField(string name, Type type, bool isReadOnly = false, bool isRequired = false, object? defaultValue = null)`
- `RemoveField(string name)` / `ClearFields()`

### Constructors
- `WithParameterlessConstructor()` / `WithoutParameterlessConstructor()`
- `WithMinimalConstructor()` / `WithoutMinimalConstructor()`
- `WithFullConstructor()` / `WithoutFullConstructor()`

### Accessors & Indexers
- `WithGetByName(string? name = null)` / `WithoutGetByName()`
- `WithGenericGetByName(string? name = null)` / `WithoutGenericGetByName()`
- `AddGetTypeByName(Type type)` / `AddGetTypeByName(string name, Type type)` / `DoAddAllGetTypeByNameMethods()`
- `WithSetByName(string? name = null)` / `WithoutSetByName()`
- `WithGenericSetByName(string? name = null)` / `WithoutGenericSetByName()`
- `AddSetTypeByName(Type type)` / `AddSetTypeByName(string name, Type type)` / `DoAddAllSetTypeByNameMethods()`
- `WithIndexer(bool setter = true)` / `WithoutIndexer()`

### Custom Methods
- `AddCustomMethod(string name, Func<IEasyTypeMethodBuilder, IEasyTypeMethodBuilder> builder)`
- `AddMethodFromExpression(string name, LambdaExpression expression)` *(via Hyperbee extension)*

---

## Performance & Design Notes

1. **Direct CIL Emission**: Generated methods compile straight into Common Intermediate Language, achieving execution speeds equivalent to hand-written C# code.
2. **Switch-Based Member Dispatch**: Lookups in `GetByName`, `SetByName`, and indexers emit IL string equality jump tables rather than invoking slow runtime reflection.
3. **Thread Safety & Immutability**: All builder modifications return new immutable configurations, guaranteeing safe concurrent usage across threads.

---

## License

This project is licensed under the [MIT License](LICENSE).
