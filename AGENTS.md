# AGENTS.md

## Repository Overview

`EasyTypeFactory` is a high-throughput, fluent dynamic type generation library for .NET (.NET 8.0, 9.0, 10.0, and .NET Standard 2.1). It synthesizes runtime types using direct CIL emission (`System.Reflection.Emit`) to produce optimized dynamic objects, DTOs, and interface implementations with minimal runtime overhead.

The repository consists of four primary projects:
- **`EasyTypeFactory`**: The core library containing fluent builder abstractions, immutable configuration models, and MSIL emitters.
- **`EasyTypeFactory.HyperbeeExpressionCompiler`**: An extension package integrating `Hyperbee.Expressions.Compiler` for compiling LINQ `Expression` trees into emitted type methods without manual IL writing.
- **`EasyTypeFactory.UnitTests`**: Unit test suite (xUnit v3) validating builder state transitions, configuration models, and isolated emitter components.
- **`EasyTypeFactory.IntegrationTests`**: End-to-end runtime integration test suite validating real instance creation, property access, interface implementation, inheritance, constructor invocation, and equality.

---

## Architecture & Code Organization

```
EasyTypeFactory/
├── Attributes/                      # Custom attributes emitted on generated types (e.g. SetsRequiredMembers)
├── Emission/                        # Low-level MSIL generation via ILGenerator
│   ├── ConstructorEmitter.cs        # Parameterless, minimal, and full constructor emission
│   ├── CustomMethodEmitter.cs       # Custom IL method definition
│   ├── EqualityEmitter.cs           # IEquatable<T>, Equals, GetHashCode, ==, !=
│   ├── GetByNameEmitter.cs          # Switch-based name dispatch for getters
│   ├── IndexerEmitter.cs            # this[string] indexer emission
│   ├── PropertyEmitter.cs           # Standard property and backing field emission
│   ├── SetByNameEmitter.cs          # Object-based SetByName dispatch
│   ├── SetTByNameEmitter.cs         # Generic SetByName<T> dispatch
│   ├── SetTypeByNameEmitter.cs      # Specialized Set{Type}ByName dispatch
│   └── Utils.cs                     # Emission helpers (stack handling, string comparison)
├── Model/                           # Immutable models representing type metadata
│   ├── ModelCustomMethod.cs
│   ├── ModelField.cs
│   ├── ModelMethod.cs
│   └── ModelProperty.cs
├── Utils/                           # Shared utility methods
├── EasyTypeBuilder.cs               # Fluent builder implementation
└── IEasyTypeBuilder.cs              # Fluent builder interface contracts
```

### Core Architectural Invariants

1. **Immutability of Builders**:
   - `IEasyTypeBuilder` and all configuration models are immutable.
   - Any method mutating configuration (e.g., `.AddField(...)`, `.WithBaseType(...)`) must return a new instance or an updated immutable record copy rather than modifying internal state in place.
2. **Switch-Based Fast-Path Member Dispatch**:
   - Dynamic member lookups (`GetByName`, `SetByName`, `this[string]`) do not use runtime reflection.
   - Emitters generate IL switch jump tables matching property/field names (with support for case-sensitive and case-insensitive string comparisons) directly against backing fields.
3. **Target Framework Compatibility & Support Policy**:
   - **Supported .NET Versions**: Officially supports active .NET versions supported by Microsoft (currently .NET 8, 9, and 10).
   - **Lifecycle & EOL Policy**: When Microsoft drops official support for a specific .NET version, support will continue for at least two additional months before being dropped.
   - **.NET Standard 2.1 Contract**: The core `EasyTypeFactory` library targets `netstandard2.1` as an API contract (`netstandard2.0` is not supported due to required reflection and runtime APIs). Although `netstandard2.1` is included as an API contract, runtimes outside Microsoft's official .NET support lifecycle (e.g., .NET Core 3.0) can consume the library but are not officially supported.
   - **Hyperbee Extension**: `EasyTypeFactory.HyperbeeExpressionCompiler` targets `net8.0`, `net9.0`, and `net10.0` (it does not target `netstandard2.1` because `Hyperbee.Expressions.Compiler` only targets .NET 8, 9, and 10).
   - Ensure new language constructs remain compatible with configured target frameworks and SDKs.

---

## Build and Test Commands

### Full Build
```bash
dotnet build
```

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Suite or Filter
```bash
# Run unit tests only
dotnet test EasyTypeFactory.UnitTests

# Run integration tests only
dotnet test EasyTypeFactory.IntegrationTests

# Run specific test by name or filter
dotnet test --filter "FullyQualifiedName~GetByNameTests"
```

---

## Coding Standards & Conventions

### Language & Formatting
- **C# Language Version**: Modern C# / .NET 10 features (file-scoped namespaces, collection expressions, pattern matching, records, extension blocks).
- **Nullability**: Explicit nullable reference types are enabled (`<Nullable>enable</Nullable>`). Never suppress nullable warnings without explicit justification; annotate reference types accurately (`string?`, `Type?`, etc.).
- **Usings & Imports**: Maintain clean, sorted `using` directives at the top of files. Keep namespaces organized according to project layer (`Model`, `Emission`, `Utils`, `Attributes`).
- **Internal Visibility**: `EasyTypeFactory.csproj` exposes internals to `EasyTypeFactory.UnitTests` and `EasyTypeFactory.IntegrationTests` via `<InternalsVisibleTo>`.

### Emitter Rules (MSIL / `System.Reflection.Emit`)
When creating or modifying emitters in `Emission/`:
- **Stack Balance**: Ensure every IL execution path balances the evaluation stack correctly before executing `OpCodes.Ret`.
- **Value Type Boxing/Unboxing**: Unbox or cast appropriately when converting between `object` and value types (`OpCodes.Unbox_Any` vs `OpCodes.Castclass`).
- **Short Branching**: Use branch labels with appropriate short-form or long-form jump opcodes (`OpCodes.Br_S`, `OpCodes.Brtrue_S`, `OpCodes.Leave_S`, etc.).
- **String Comparisons**: Respect case-sensitivity configuration flags by invoking `string.Equals(string, string, StringComparison)` when case-insensitivity is active.

---

## Agent Guidelines for Implementing Changes

When assigned a task in this repository, follow this workflow to maximize efficiency and correctness:

1. **Understand Scope**:
   - If adding a fluent builder method, update both `IEasyTypeBuilder` and `EasyTypeBuilder`.
   - If introducing new member definitions, create/update the corresponding model in `Model/`.
   - If altering generated IL, locate the responsible emitter in `Emission/` or create a new dedicated emitter class.
2. **Preserve Immutability**:
   - Ensure builder transitions return new state instances. Never mutate existing collections in place.
3. **Verify with Integration Tests**:
   - Unit tests verify model integrity and builder transitions.
   - Any new runtime behavior (new constructor shape, custom member, accessor, or operator) **must** be validated with an integration test that builds the type, instantiates it via reflection or dynamic invocation, and asserts runtime execution behavior.
4. **Run Verification Before Completion**:
   - Always run `dotnet test` to confirm zero regressions across all test suites.
