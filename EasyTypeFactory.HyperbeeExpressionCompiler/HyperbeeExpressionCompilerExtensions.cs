using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Expressions.Compiler;

namespace EasyTypeFactory.HyperbeeExpressionCompiler;

public static class HyperbeeExpressionCompilerExtensions
{
    extension(IEasyTypeBuilder builder)
    {
        public IEasyTypeBuilder AddMethodFromExpression(
            string name,
            LambdaExpression expression)
        {
            return builder.AddCustomMethod(name, customMethodBuilder =>
                customMethodBuilder
                    .TypesFromExpression(expression)
                    .WithEmitter((_, context) => 
                        HyperbeeCompiler.CompileToInstanceMethod(expression, context.MethodBuilder)));
        }

        public IEasyTypeBuilder AddMethodFromExpression(
            string name,
            MethodAttributes attributes,
            LambdaExpression expression)
        {
            return builder.AddCustomMethod(name, customMethodBuilder =>
                customMethodBuilder
                    .TypesFromExpression(expression)
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

        public IEasyTypeBuilder AddMethodFromExpression(
            string name,
            Func<IEasyTypeMethodBuilder, IEasyTypeMethodBuilder> methodBuilder,
            LambdaExpression expression)
        {
            return builder.AddCustomMethod(name, customMethodBuilder =>
            {
                customMethodBuilder
                    .TypesFromExpression(expression)
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

                return methodBuilder(customMethodBuilder);
            });
        }
    }

    extension(IEasyTypeMethodBuilder builder)
    {
        private IEasyTypeMethodBuilder TypesFromExpression(LambdaExpression expression)
        {
            return builder
                .WithParameterTypes([.. expression.Parameters.Select(p => p.Type)])
                .WithReturnType(expression.ReturnType);
        }
    }
}
