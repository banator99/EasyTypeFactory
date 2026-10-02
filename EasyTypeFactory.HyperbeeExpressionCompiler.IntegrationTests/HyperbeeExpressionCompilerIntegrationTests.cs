using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using EasyTypeFactory;
using EasyTypeFactory.HyperbeeExpressionCompiler;
using Xunit;

namespace EasyTypeFactory.HyperbeeExpressionCompiler.IntegrationTests;

public interface ICalculatorService
{
    int Add(int a, int b);
    int Subtract(int a, int b);
    int Multiply(int a, int b);
    double Divide(double a, double b);
    string Greet(string name);
}

public abstract class BaseProcessor
{
    public abstract string Process(string input);
}

public class Person
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class HyperbeeExpressionCompilerIntegrationTests
{
    [Fact]
    public void InstanceMethod_Parameterless_ReturnsExpectedValue()
    {
        Expression<Func<string>> expr = () => "Hello from Hyperbee";

        var type = EasyTypeBuilder.Create()
            .WithParameterlessConstructor()
            .AddMethodFromExpression("GetMessage", expr)
            .Build();

        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("GetMessage", Type.EmptyTypes)!;

        Assert.NotNull(method);
        var result = method.Invoke(instance, null);
        Assert.Equal("Hello from Hyperbee", result);
    }

    [Fact]
    public void InstanceMethod_PrimitiveParameters_ExecutesCorrectly()
    {
        Expression<Func<object, int, int, int>> addExpr = (@this, a, b) => a + b;

        var type = EasyTypeBuilder.Create()
            .WithParameterlessConstructor()
            .AddMethodFromExpression("Add", addExpr)
            .Build();

        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("Add", [typeof(int), typeof(int)])!;

        Assert.NotNull(method);
        var result = (int)method.Invoke(instance, [14, 28])!;
        Assert.Equal(42, result);
    }

    [Fact]
    public void InstanceMethod_MultipleParametersAndTypes_ExecutesCorrectly()
    {
        Expression<Func<object, string, int, bool, string>> formatExpr =
            (@this, prefix, count, active) => $"{prefix}: {count} (active={active})";

        var type = EasyTypeBuilder.Create()
            .WithParameterlessConstructor()
            .AddMethodFromExpression("FormatInfo", formatExpr)
            .Build();

        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("FormatInfo", [typeof(string), typeof(int), typeof(bool)])!;

        Assert.NotNull(method);
        var result = method.Invoke(instance, ["Items", 5, true]);
        Assert.Equal("Items: 5 (active=True)", result);
    }

    [Fact]
    public void InstanceMethod_VoidReturn_ExecutesSideEffect()
    {
        var sideEffects = new List<string>();
        Expression<Action<object, List<string>, string>> appendExpr = (@this, list, item) => list.Add(item);

        var type = EasyTypeBuilder.Create()
            .WithParameterlessConstructor()
            .AddMethodFromExpression("AppendItem", appendExpr)
            .Build();

        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("AppendItem", [typeof(List<string>), typeof(string)])!;

        Assert.NotNull(method);
        method.Invoke(instance, [sideEffects, "FirstEntry"]);

        Assert.Single(sideEffects);
        Assert.Equal("FirstEntry", sideEffects[0]);
    }

    [Fact]
    public void StaticMethod_WithAttributes_ExecutesCorrectly()
    {
        Expression<Func<int, int, int>> multiplyExpr = (x, y) => x * y;

        var type = EasyTypeBuilder.Create()
            .AddMethodFromExpression(
                "Multiply",
                MethodAttributes.Public | MethodAttributes.Static,
                multiplyExpr)
            .Build();

        var method = type.GetMethod("Multiply", BindingFlags.Public | BindingFlags.Static)!;

        Assert.NotNull(method);
        var result = (int)method.Invoke(null, [6, 7])!;
        Assert.Equal(42, result);
    }

    [Fact]
    public void StaticMethod_Parameterless_ReturnsExpectedValue()
    {
        Expression<Func<int>> answerExpr = () => 42;

        var type = EasyTypeBuilder.Create()
            .AddMethodFromExpression(
                "GetAnswer",
                MethodAttributes.Public | MethodAttributes.Static,
                answerExpr)
            .Build();

        var method = type.GetMethod("GetAnswer", BindingFlags.Public | BindingFlags.Static)!;

        Assert.NotNull(method);
        var result = (int)method.Invoke(null, null)!;
        Assert.Equal(42, result);
    }

    [Fact]
    public void MethodBuilderCallback_StaticMethod_ExecutesCorrectly()
    {
        Expression<Func<int>> constantExpr = () => 999;

        var type = EasyTypeBuilder.Create()
            .AddMethodFromExpression(
                "GetConstant",
                mb => mb.WithAttributes(MethodAttributes.Public | MethodAttributes.Static),
                constantExpr)
            .Build();

        var method = type.GetMethod("GetConstant", BindingFlags.Public | BindingFlags.Static)!;

        Assert.NotNull(method);
        var result = (int)method.Invoke(null, null)!;
        Assert.Equal(999, result);
    }

    [Fact]
    public void MethodBuilderCallback_VirtualInstanceMethod_ExecutesCorrectly()
    {
        Expression<Func<object, string, string>> transformExpr = (@this, s) => s.ToUpperInvariant();

        var type = EasyTypeBuilder.Create()
            .WithParameterlessConstructor()
            .AddMethodFromExpression(
                "Transform",
                mb => mb.WithAttributes(MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig),
                transformExpr)
            .Build();

        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("Transform", [typeof(string)])!;

        Assert.NotNull(method);
        Assert.True(method.IsVirtual);
        var result = (string)method.Invoke(instance, ["hello"])!;
        Assert.Equal("HELLO", result);
    }

    [Fact]
    public void MethodBuilderCallback_WithModifier_AppliesModifierAndExecutes()
    {
        var modifierRan = false;
        Expression<Func<object, int, int>> squareExpr = (@this, x) => x * x;

        var type = EasyTypeBuilder.Create()
            .WithParameterlessConstructor()
            .AddMethodFromExpression(
                "Square",
                mb => mb.ModifyMethod((m, ctx) =>
                {
                    modifierRan = true;
                    Assert.NotNull(m);
                    Assert.NotNull(ctx);
                }),
                squareExpr)
            .Build();

        Assert.True(modifierRan);

        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("Square", [typeof(int)])!;
        Assert.NotNull(method);

        var result = (int)method.Invoke(instance, [9])!;
        Assert.Equal(81, result);
    }

    [Fact]
    public void MethodBuilderCallback_NonPublicMethod_CanBeInvokedViaReflection()
    {
        Expression<Func<object, string, string>> secretExpr = (@this, s) => "Secret: " + s;

        var type = EasyTypeBuilder.Create()
            .WithParameterlessConstructor()
            .AddMethodFromExpression(
                "GetSecret",
                mb => mb.WithAttributes(MethodAttributes.Private | MethodAttributes.HideBySig),
                secretExpr)
            .Build();

        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("GetSecret", BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.NotNull(method);
        var result = (string)method.Invoke(instance, ["classified"])!;
        Assert.Equal("Secret: classified", result);
    }

    [Fact]
    public void InterfaceImplementation_AllMethodsImplemented_DispatchesCorrectly()
    {
        const MethodAttributes interfaceMethodAttrs =
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.HideBySig;

        var type = EasyTypeBuilder.Create()
            .WithParameterlessConstructor()
            .AddInterface(typeof(ICalculatorService))
            .AddMethodFromExpression("Add", interfaceMethodAttrs, (Expression<Func<object, int, int, int>>)((@this, a, b) => a + b))
            .AddMethodFromExpression("Subtract", interfaceMethodAttrs, (Expression<Func<object, int, int, int>>)((@this, a, b) => a - b))
            .AddMethodFromExpression("Multiply", interfaceMethodAttrs, (Expression<Func<object, int, int, int>>)((@this, a, b) => a * b))
            .AddMethodFromExpression("Divide", interfaceMethodAttrs, (Expression<Func<object, double, double, double>>)((@this, a, b) => a / b))
            .AddMethodFromExpression("Greet", interfaceMethodAttrs, (Expression<Func<object, string, string>>)((@this, name) => $"Hello, {name}!"))
            .Build();

        var instance = (ICalculatorService)Activator.CreateInstance(type)!;

        Assert.Equal(15, instance.Add(10, 5));
        Assert.Equal(5, instance.Subtract(10, 5));
        Assert.Equal(50, instance.Multiply(10, 5));
        Assert.Equal(2.5, instance.Divide(10.0, 4.0));
        Assert.Equal("Hello, Alice!", instance.Greet("Alice"));
    }

    [Fact]
    public void BaseClassImplementation_OverridesAbstractMethod()
    {
        const MethodAttributes overrideAttrs =
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig;

        var type = EasyTypeBuilder.Create()
            .WithParameterlessConstructor()
            .WithBaseType(typeof(BaseProcessor))
            .AddMethodFromExpression("Process", overrideAttrs, (Expression<Func<object, string, string>>)((@this, s) => s.ToUpperInvariant()))
            .Build();

        var instance = (BaseProcessor)Activator.CreateInstance(type)!;

        Assert.Equal("DYNAMIC OUTPUT", instance.Process("dynamic output"));
    }

    [Fact]
    public void ComplexExpressions_ConditionalAndComplexObjects_ExecutesCorrectly()
    {
        Expression<Func<object, Person, string>> describePerson = (@this, p) =>
            p.Age >= 18 ? $"{p.Name} is an adult" : $"{p.Name} is a minor";

        Expression<Func<object, int?, int?, int>> addNullable = (@this, a, b) =>
            (a ?? 0) + (b ?? 0);

        var type = EasyTypeBuilder.Create()
            .WithParameterlessConstructor()
            .AddMethodFromExpression("Describe", describePerson)
            .AddMethodFromExpression("AddNullable", addNullable)
            .Build();

        var instance = Activator.CreateInstance(type)!;

        var describeMethod = type.GetMethod("Describe", [typeof(Person)])!;
        var addNullableMethod = type.GetMethod("AddNullable", [typeof(int?), typeof(int?)])!;

        var adultResult = describeMethod.Invoke(instance, [new Person { Name = "Bob", Age = 25 }]);
        var minorResult = describeMethod.Invoke(instance, [new Person { Name = "Charlie", Age = 15 }]);

        Assert.Equal("Bob is an adult", adultResult);
        Assert.Equal("Charlie is a minor", minorResult);

        var nullSum1 = addNullableMethod.Invoke(instance, [null, 10]);
        var nullSum2 = addNullableMethod.Invoke(instance, [5, 15]);
        var nullSum3 = addNullableMethod.Invoke(instance, [null, null]);

        Assert.Equal(10, nullSum1);
        Assert.Equal(20, nullSum2);
        Assert.Equal(0, nullSum3);
    }

    [Fact]
    public void ComplexExpressions_BlockExpressions_ExecutesCorrectly()
    {
        var paramThis = Expression.Parameter(typeof(object), "this");
        var paramA = Expression.Parameter(typeof(int), "a");
        var paramB = Expression.Parameter(typeof(int), "b");
        var varSum = Expression.Variable(typeof(int), "sum");
        var block = Expression.Block(
            [varSum],
            Expression.Assign(varSum, Expression.Add(paramA, paramB)),
            Expression.Multiply(varSum, Expression.Constant(3))
        );
        var lambda = Expression.Lambda<Func<object, int, int, int>>(block, paramThis, paramA, paramB);

        var type = EasyTypeBuilder.Create()
            .WithParameterlessConstructor()
            .AddMethodFromExpression("ComputeCustom", lambda)
            .Build();

        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("ComputeCustom", [typeof(int), typeof(int)])!;

        Assert.NotNull(method);
        var result = (int)method.Invoke(instance, [4, 6])!;
        Assert.Equal(30, result);
    }

    [Fact]
    public void TypeWithFieldsAndExpressionMethods_CombinesSeamlessly()
    {
        Expression<Func<object, int, int>> calculateTax = (@this, amount) => (int)(amount * 0.2);

        var type = EasyTypeBuilder.Create("InvoiceModel")
            .WithParameterlessConstructor()
            .AddField("Description", typeof(string))
            .AddField("Amount", typeof(int))
            .AddMethodFromExpression("CalculateTax", calculateTax)
            .Build();

        var instance = Activator.CreateInstance(type)!;

        var descField = type.GetField("Description")!;
        var amountField = type.GetField("Amount")!;
        var calcMethod = type.GetMethod("CalculateTax", [typeof(int)])!;

        descField.SetValue(instance, "Consulting Services");
        amountField.SetValue(instance, 1000);

        Assert.Equal("Consulting Services", descField.GetValue(instance));
        Assert.Equal(1000, amountField.GetValue(instance));

        var tax = (int)calcMethod.Invoke(instance, [1000])!;
        Assert.Equal(200, tax);
    }

    [Fact]
    public void MultipleExpressionMethods_InstanceAndStatic_CoexistOnSameType()
    {
        var type = EasyTypeBuilder.Create()
            .WithParameterlessConstructor()
            .AddMethodFromExpression("Add", (Expression<Func<object, int, int, int>>)((@this, a, b) => a + b))
            .AddMethodFromExpression("Subtract", (Expression<Func<object, int, int, int>>)((@this, a, b) => a - b))
            .AddMethodFromExpression("Version", MethodAttributes.Public | MethodAttributes.Static, (Expression<Func<string>>)(() => "1.0.0"))
            .AddMethodFromExpression("Max", MethodAttributes.Public | MethodAttributes.Static, (Expression<Func<int, int, int>>)((a, b) => Math.Max(a, b)))
            .Build();

        var instance = Activator.CreateInstance(type)!;

        var addMethod = type.GetMethod("Add", [typeof(int), typeof(int)])!;
        var subMethod = type.GetMethod("Subtract", [typeof(int), typeof(int)])!;
        var verMethod = type.GetMethod("Version", BindingFlags.Public | BindingFlags.Static)!;
        var maxMethod = type.GetMethod("Max", BindingFlags.Public | BindingFlags.Static)!;

        Assert.Equal(12, addMethod.Invoke(instance, [7, 5]));
        Assert.Equal(2, subMethod.Invoke(instance, [7, 5]));
        Assert.Equal("1.0.0", verMethod.Invoke(null, null));
        Assert.Equal(99, maxMethod.Invoke(null, [42, 99]));
    }
}
