using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace EasyTypeFactory.HyperbeeExpressionCompiler.UnitTests;

public class HyperbeeExpressionCompilerExtensionsTests
{
    private sealed class MockEasyTypeBuilder : IEasyTypeBuilder
    {
        public string? CapturedMethodName { get; private set; }
        public Func<IEasyTypeMethodBuilder, IEasyTypeMethodBuilder>? CapturedBuilderFunc { get; private set; }

        public IEasyTypeBuilder AddCustomMethod(
            string methodName,
            Func<IEasyTypeMethodBuilder, IEasyTypeMethodBuilder> builder)
        {
            CapturedMethodName = methodName;
            CapturedBuilderFunc = builder;
            return this;
        }

        public IEasyTypeBuilder Defaults() => throw new NotSupportedException();
        public IEasyTypeBuilder WithModuleBuilder(ModuleBuilder moduleBuilder) => throw new NotSupportedException();
        public IEasyTypeBuilder WithoutModuleBuilder() => throw new NotSupportedException();
        public IEasyTypeBuilder WithName(string? name, bool discriminate = true) => throw new NotSupportedException();
        public IEasyTypeBuilder WithoutName() => throw new NotSupportedException();
        public IEasyTypeBuilder WithBaseType(Type baseType) => throw new NotSupportedException();
        public IEasyTypeBuilder WithoutBaseType() => throw new NotSupportedException();
        public IEasyTypeBuilder AddField(string name, Type type, bool isReadOnly = false, bool isRequired = false, object? defaultValue = null) => throw new NotSupportedException();
        public IEasyTypeBuilder RemoveField(string name) => throw new NotSupportedException();
        public IEasyTypeBuilder ClearFields() => throw new NotSupportedException();
        public IEasyTypeBuilder AddInterface(Type interfaceType) => throw new NotSupportedException();
        public IEasyTypeBuilder RemoveInterface(Type interfaceType) => throw new NotSupportedException();
        public IEasyTypeBuilder ClearInterfaces() => throw new NotSupportedException();
        public IEasyTypeBuilder WithGetByName(string? methodName = null) => throw new NotSupportedException();
        public IEasyTypeBuilder WithoutGetByName() => throw new NotSupportedException();
        public IEasyTypeBuilder WithGenericGetByName(string? methodName = null) => throw new NotSupportedException();
        public IEasyTypeBuilder WithoutGenericGetByName() => throw new NotSupportedException();
        public IEasyTypeBuilder AddGetTypeByName(Type type) => throw new NotSupportedException();
        public IEasyTypeBuilder AddGetTypeByName(string name, Type type) => throw new NotSupportedException();
        public IEasyTypeBuilder RemoveGetTypeByName(Type type) => throw new NotSupportedException();
        public IEasyTypeBuilder ClearGetTypeByNames() => throw new NotSupportedException();
        public IEasyTypeBuilder DoAddAllGetTypeByNameMethods() => throw new NotSupportedException();
        public IEasyTypeBuilder DoNotAddAllGetTypeByNameMethods() => throw new NotSupportedException();
        public IEasyTypeBuilder WithSetByName(string? methodName = null) => throw new NotSupportedException();
        public IEasyTypeBuilder WithoutSetByName() => throw new NotSupportedException();
        public IEasyTypeBuilder WithGenericSetByName(string? methodName = null) => throw new NotSupportedException();
        public IEasyTypeBuilder WithoutGenericSetByName() => throw new NotSupportedException();
        public IEasyTypeBuilder AddSetTypeByName(Type type) => throw new NotSupportedException();
        public IEasyTypeBuilder AddSetTypeByName(string name, Type type) => throw new NotSupportedException();
        public IEasyTypeBuilder RemoveSetTypeByName(Type type) => throw new NotSupportedException();
        public IEasyTypeBuilder ClearSetTypeByNames() => throw new NotSupportedException();
        public IEasyTypeBuilder DoAddAllSetTypeByNameMethods() => throw new NotSupportedException();
        public IEasyTypeBuilder DoNotAddAllSetTypeByNameMethods() => throw new NotSupportedException();
        public IEasyTypeBuilder WithIndexer(bool setter = true) => throw new NotSupportedException();
        public IEasyTypeBuilder WithoutIndexer() => throw new NotSupportedException();
        public IEasyTypeBuilder RemoveCustomMethods(string methodName) => throw new NotSupportedException();
        public IEasyTypeBuilder ClearCustomMethods() => throw new NotSupportedException();
        public IEasyTypeBuilder WithParameterlessConstructor() => throw new NotSupportedException();
        public IEasyTypeBuilder WithoutParameterlessConstructor() => throw new NotSupportedException();
        public IEasyTypeBuilder WithMinimalConstructor() => throw new NotSupportedException();
        public IEasyTypeBuilder WithoutMinimalConstructor() => throw new NotSupportedException();
        public IEasyTypeBuilder WithFullConstructor() => throw new NotSupportedException();
        public IEasyTypeBuilder WithoutFullConstructor() => throw new NotSupportedException();
        public IEasyTypeBuilder WithEquality(bool operators = true) => throw new NotSupportedException();
        public IEasyTypeBuilder WithoutEquality() => throw new NotSupportedException();
        public IEasyTypeBuilder CaseSensitive() => throw new NotSupportedException();
        public IEasyTypeBuilder CaseInsensitive() => throw new NotSupportedException();
        public Type Build() => throw new NotSupportedException();
        public bool Equals(EasyTypeBuilder? other) => throw new NotSupportedException();
    }

    [Fact]
    public void AddMethodFromExpression_NameAndExpression_InstanceMethod_ConfiguresMethodDefinitionCorrectly()
    {
        var mockBuilder = new MockEasyTypeBuilder();
        Expression<Func<object, int, int, int>> expression = (@this, a, b) => a + b;

        var result = mockBuilder.AddMethodFromExpression("Add", expression);

        Assert.Same(mockBuilder, result);
        Assert.Equal("Add", mockBuilder.CapturedMethodName);
        Assert.NotNull(mockBuilder.CapturedBuilderFunc);

        var methodBuilder = new CustomMethodBuilder("Add");
        var configuredMethodBuilder = (CustomMethodBuilder)mockBuilder.CapturedBuilderFunc(methodBuilder);
        var def = configuredMethodBuilder.ValidateAndGetDefinition();

        Assert.Equal("Add", def.Name);
        Assert.Equal(typeof(int), def.ReturnType);
        Assert.Equal([typeof(int), typeof(int)], def.ParameterTypes);
        Assert.Equal(MethodAttributes.Public, def.Attributes);
        Assert.NotNull(def.Emitter);
    }

    [Fact]
    public void AddMethodFromExpression_WithAttributes_InstanceMethod_ConfiguresMethodDefinitionCorrectly()
    {
        var mockBuilder = new MockEasyTypeBuilder();
        Expression<Func<object, string, string>> expression = (@this, s) => s.ToUpperInvariant();
        var attributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig;

        var result = mockBuilder.AddMethodFromExpression("Transform", attributes, expression);

        Assert.Same(mockBuilder, result);
        Assert.Equal("Transform", mockBuilder.CapturedMethodName);
        Assert.NotNull(mockBuilder.CapturedBuilderFunc);

        var methodBuilder = new CustomMethodBuilder("Transform");
        var configuredMethodBuilder = (CustomMethodBuilder)mockBuilder.CapturedBuilderFunc(methodBuilder);
        var def = configuredMethodBuilder.ValidateAndGetDefinition();

        Assert.Equal("Transform", def.Name);
        Assert.Equal(typeof(string), def.ReturnType);
        Assert.Equal([typeof(string)], def.ParameterTypes);
        Assert.Equal(attributes, def.Attributes);
        Assert.NotNull(def.Emitter);
    }

    [Fact]
    public void AddMethodFromExpression_WithAttributes_StaticMethod_ConfiguresMethodDefinitionCorrectly()
    {
        var mockBuilder = new MockEasyTypeBuilder();
        Expression<Func<double, double, double>> expression = (x, y) => Math.Pow(x, y);
        var attributes = MethodAttributes.Public | MethodAttributes.Static;

        var result = mockBuilder.AddMethodFromExpression("Power", attributes, expression);

        Assert.Same(mockBuilder, result);
        Assert.Equal("Power", mockBuilder.CapturedMethodName);
        Assert.NotNull(mockBuilder.CapturedBuilderFunc);

        var methodBuilder = new CustomMethodBuilder("Power");
        var configuredMethodBuilder = (CustomMethodBuilder)mockBuilder.CapturedBuilderFunc(methodBuilder);
        var def = configuredMethodBuilder.ValidateAndGetDefinition();

        Assert.Equal("Power", def.Name);
        Assert.Equal(typeof(double), def.ReturnType);
        Assert.Equal([typeof(double), typeof(double)], def.ParameterTypes);
        Assert.Equal(attributes, def.Attributes);
        Assert.NotNull(def.Emitter);
    }

    [Fact]
    public void AddMethodFromExpression_WithMethodBuilderFunc_AppliesCustomizations()
    {
        var mockBuilder = new MockEasyTypeBuilder();
        Expression<Func<object, int, string>> expression = (@this, i) => i.ToString();

        var result = mockBuilder.AddMethodFromExpression(
            "IntToStr",
            mb => mb.WithAttributes(MethodAttributes.Private | MethodAttributes.HideBySig)
                    .ModifyMethod((_, _) => { }),
            expression);

        Assert.Same(mockBuilder, result);
        Assert.Equal("IntToStr", mockBuilder.CapturedMethodName);
        Assert.NotNull(mockBuilder.CapturedBuilderFunc);

        var methodBuilder = new CustomMethodBuilder("IntToStr");
        var configuredMethodBuilder = (CustomMethodBuilder)mockBuilder.CapturedBuilderFunc(methodBuilder);
        var def = configuredMethodBuilder.ValidateAndGetDefinition();

        Assert.Equal("IntToStr", def.Name);
        Assert.Equal(typeof(string), def.ReturnType);
        Assert.Equal([typeof(int)], def.ParameterTypes);
        Assert.Equal(MethodAttributes.Private | MethodAttributes.HideBySig, def.Attributes);
        Assert.Single(def.Modifiers);
        Assert.NotNull(def.Emitter);
    }

    [Fact]
    public void AddMethodFromExpression_WithMethodBuilderFunc_StaticMethod_ConfiguresMethodDefinitionCorrectly()
    {
        var mockBuilder = new MockEasyTypeBuilder();
        Expression<Func<int>> expression = () => 999;

        var result = mockBuilder.AddMethodFromExpression(
            "GetConstant",
            mb => mb.WithAttributes(MethodAttributes.Public | MethodAttributes.Static),
            expression);

        Assert.Same(mockBuilder, result);
        Assert.Equal("GetConstant", mockBuilder.CapturedMethodName);
        Assert.NotNull(mockBuilder.CapturedBuilderFunc);

        var methodBuilder = new CustomMethodBuilder("GetConstant");
        var configuredMethodBuilder = (CustomMethodBuilder)mockBuilder.CapturedBuilderFunc(methodBuilder);
        var def = configuredMethodBuilder.ValidateAndGetDefinition();

        Assert.Equal("GetConstant", def.Name);
        Assert.Equal(typeof(int), def.ReturnType);
        Assert.NotNull(def.ParameterTypes);
        Assert.Empty(def.ParameterTypes);
        Assert.Equal(MethodAttributes.Public | MethodAttributes.Static, def.Attributes);
        Assert.NotNull(def.Emitter);
    }

    [Fact]
    public void AddMethodFromExpression_VoidParameterlessExpression_ExtractsTypesCorrectly()
    {
        var mockBuilder = new MockEasyTypeBuilder();
        Expression<Action> expression = () => Console.WriteLine();

        mockBuilder.AddMethodFromExpression("DoNothing", expression);

        var methodBuilder = new CustomMethodBuilder("DoNothing");
        var configuredMethodBuilder = (CustomMethodBuilder)mockBuilder.CapturedBuilderFunc!(methodBuilder);
        var def = configuredMethodBuilder.ValidateAndGetDefinition();

        Assert.Equal("DoNothing", def.Name);
        Assert.Equal(typeof(void), def.ReturnType);
        Assert.NotNull(def.ParameterTypes);
        Assert.Empty(def.ParameterTypes);
        Assert.NotNull(def.Emitter);
    }

    [Fact]
    public void AddMethodFromExpression_InstanceParameterlessWithThis_ExtractsTypesCorrectly()
    {
        var mockBuilder = new MockEasyTypeBuilder();
        Expression<Func<object, string>> expression = @this => "Hello";

        mockBuilder.AddMethodFromExpression("GetHello", expression);

        var methodBuilder = new CustomMethodBuilder("GetHello");
        var configuredMethodBuilder = (CustomMethodBuilder)mockBuilder.CapturedBuilderFunc!(methodBuilder);
        var def = configuredMethodBuilder.ValidateAndGetDefinition();

        Assert.Equal("GetHello", def.Name);
        Assert.Equal(typeof(string), def.ReturnType);
        Assert.NotNull(def.ParameterTypes);
        Assert.Empty(def.ParameterTypes);
        Assert.NotNull(def.Emitter);
    }

    [Fact]
    public void AddMethodFromExpression_MultipleParameters_ExtractsAllParameterTypesInOrder()
    {
        var mockBuilder = new MockEasyTypeBuilder();
        Expression<Func<object, int, string, bool, double, string>> expression =
            (@this, i, s, b, d) => $"{i}-{s}-{b}-{d}";

        mockBuilder.AddMethodFromExpression("FormatValues", expression);

        var methodBuilder = new CustomMethodBuilder("FormatValues");
        var configuredMethodBuilder = (CustomMethodBuilder)mockBuilder.CapturedBuilderFunc!(methodBuilder);
        var def = configuredMethodBuilder.ValidateAndGetDefinition();

        Assert.Equal("FormatValues", def.Name);
        Assert.Equal(typeof(string), def.ReturnType);
        Assert.Equal(
            [typeof(int), typeof(string), typeof(bool), typeof(double)],
            def.ParameterTypes);
    }

    [Fact]
    public void AddMethodFromExpression_FluentImmutability_EasyTypeBuilder_ReturnsNewBuilderInstance()
    {
        var b0 = EasyTypeBuilder.Create();
        Expression<Func<object, int, int>> square = (@this, x) => x * x;
        Expression<Func<int, int>> squareStatic = x => x * x;

        var b1 = b0.AddMethodFromExpression("Square", square);
        var b2 = b0.AddMethodFromExpression("SquareStatic", MethodAttributes.Public | MethodAttributes.Static, squareStatic);
        var b3 = b0.AddMethodFromExpression("SquareCustom", mb => mb.WithAttributes(MethodAttributes.Private), square);

        Assert.NotSame(b0, b1);
        Assert.NotSame(b0, b2);
        Assert.NotSame(b0, b3);
        Assert.NotSame(b1, b2);
        Assert.NotSame(b1, b3);
        Assert.NotSame(b2, b3);
    }
}
