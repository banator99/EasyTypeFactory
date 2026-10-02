using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Expressions.Compiler;

namespace EasyTypeFactory.HyperbeeExpressionCompiler;

public static class HyperbeeExpressionCompilerExtensions
{
    public static IEasyTypeBuilder AddMethodFromExpression(
        this IEasyTypeBuilder builder,
        string name,
        LambdaExpression expression)
    {
        return builder.AddCustomMethod(name, customMethodBuilder =>
            customMethodBuilder
                .TypesFromExpression(expression, isStatic: false)
                .WithEmitter((_, context) => 
                    HyperbeeCompiler.CompileToInstanceMethod(expression, context.MethodBuilder)));
    }

    public static IEasyTypeBuilder AddMethodFromExpression(
        this IEasyTypeBuilder builder,
        string name,
        MethodAttributes attributes,
        LambdaExpression expression)
    {
        var isStatic = (attributes & MethodAttributes.Static) == MethodAttributes.Static;

        return builder.AddCustomMethod(name, customMethodBuilder =>
            customMethodBuilder
                .TypesFromExpression(expression, isStatic)
                .WithAttributes(attributes)
                .WithEmitter((_, context) =>
                {
                    if ((attributes & MethodAttributes.Static) == MethodAttributes.Static)
                    {
                        HyperbeeCompiler.CompileToMethod(expression, context.MethodBuilder);
                    }
                    else
                    {
                        HyperbeeCompiler.CompileToInstanceMethod(expression, context.MethodBuilder);
                    }
                }));
    }

    public static IEasyTypeBuilder AddMethodFromExpression(
        this IEasyTypeBuilder builder,
        string name,
        Func<IEasyTypeMethodBuilder, IEasyTypeMethodBuilder> methodBuilder,
        LambdaExpression expression)
    {
        return builder.AddCustomMethod(name, customMethodBuilder =>
        {
            var configuredBuilder = customMethodBuilder
                .TypesFromExpression(expression, isStatic: false)
                .WithEmitter((_, context) =>
                {
                    if ((context.MethodBuilder.Attributes & MethodAttributes.Static) == MethodAttributes.Static)
                    {
                        HyperbeeCompiler.CompileToMethod(expression, context.MethodBuilder);
                    }
                    else
                    {
                        HyperbeeCompiler.CompileToInstanceMethod(expression, context.MethodBuilder);
                    }
                });

            return methodBuilder(configuredBuilder);
        });
    }
    
    private static IEasyTypeMethodBuilder TypesFromExpression(
        this IEasyTypeMethodBuilder builder,
        LambdaExpression expression,
        bool isStatic = false)
    {
        var parameterTypes = isStatic || expression.Parameters.Count == 0
            ? expression.Parameters.Select(p => p.Type)
            : expression.Parameters.Skip(1).Select(p => p.Type);

        return builder
            .WithParameterTypes([.. parameterTypes])
            .WithReturnType(expression.ReturnType);
    }
}
