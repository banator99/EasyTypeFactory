using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using static System.Reflection.Emit.OpCodes;

namespace EasyTypeFactory.Emission;

internal static class Utils
{
    private static readonly ConstructorInfo RequiredMemberAttributeCtor = 
        typeof(RequiredMemberAttribute).GetConstructor(Type.EmptyTypes)!;

    private static readonly ConstructorInfo SetsRequiredMembersAttributeCtor = 
        typeof(SetsRequiredMembersAttribute).GetConstructor(Type.EmptyTypes)!;

    private static readonly MethodInfo StringEqualsStringMethod =
        typeof(string).GetMethod(
            "Equals", 
            BindingFlags.Public | BindingFlags.Instance, 
            binder: null,
            [typeof(string)],
            modifiers: null)!;
    
    private static readonly MethodInfo StringEqualsStringStringComparisonMethod =
        typeof(string).GetMethod(
            "Equals", 
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            [typeof(string), typeof(StringComparison)],
            modifiers: null)!;
    
    public static readonly MethodInfo StringConcat3Method =
        typeof(string).GetMethod(
            "Concat", 
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            [typeof(string), typeof(string), typeof(string)],
            modifiers: null)!;
    
    private static readonly ConstructorInfo KeyNotFoundExceptionCtor =
        typeof(KeyNotFoundException).GetConstructor([typeof(string)])!;
    
    public static readonly ConstructorInfo InvalidOperationExceptionCtor =
        typeof(InvalidOperationException).GetConstructor([typeof(string)])!;
    
    private static readonly MethodInfo TypeGetTypeFromHandleMethod =
        typeof(Type).GetMethod(
            "GetTypeFromHandle", 
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            [typeof(RuntimeTypeHandle)],
            modifiers: null)!;
    
    private static readonly MethodInfo TypeOpEqualityMethod =
        typeof(Type).GetMethod(
            "op_Equality",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            [typeof(Type), typeof(Type)],
            modifiers: null)!;
    
    public static readonly MethodInfo TypeIsAssignableFromMethod =
        typeof(Type).GetMethod(
            "IsAssignableFrom", 
            BindingFlags.Public | BindingFlags.Instance, 
            binder: null,
            [typeof(Type)],
            modifiers: null)!;

    public static readonly MethodInfo HashCodeGenericAddMethod = typeof(HashCode)
        .GetMethods()
        .Single(candidate => candidate is { Name: "Add", IsGenericMethodDefinition: true } 
                             && candidate.GetParameters().Length == 1);
        
    public static readonly MethodInfo HashCodeFinishMethod = 
        typeof(HashCode).GetMethod(nameof(HashCode.ToHashCode), Type.EmptyTypes)!;
    
    public static readonly MethodInfo ObjectGetHashCodeMethod = 
        typeof(object).GetMethod(nameof(GetHashCode), Type.EmptyTypes)!;

    public static readonly MethodInfo ObjectEqualsMethod =
        typeof(object).GetMethod("Equals", [typeof(object)])!;
    
    public static CustomAttributeBuilder RequiredMemberAttribute => new(RequiredMemberAttributeCtor, []);
    
    public static CustomAttributeBuilder SetsRequiredMembersAttribute => new(SetsRequiredMembersAttributeCtor, []);

    public static string GetFullName(Type type)
    {
        return type.FullName ?? type.Name;
    }

    public static void EmitStringEquals(ILGenerator il, int arg, string value, bool caseInsensitive)
    {
        il.Emit(Ldstr, value);
        il.Emit(Ldarg, arg);
        
        if (caseInsensitive)
        {
            il.Emit(Ldc_I4, (int)StringComparison.OrdinalIgnoreCase);
            il.Emit(Call, StringEqualsStringStringComparisonMethod);
        }
        else
        {
            il.Emit(Call, StringEqualsStringMethod);
        }
    }
    
    public static void EmitThrowFieldNotFoundException(ILGenerator il, int arg)
    {
        il.Emit(Ldstr, "No field with name '");
        il.Emit(Ldarg, arg);
        il.Emit(Ldstr, "' was found.");
        il.Emit(Call, StringConcat3Method);
        il.Emit(Newobj, KeyNotFoundExceptionCtor);
        il.Emit(Throw);
    }
    
    public static void EmitThrowFieldTypeNotCompatibleException(
        ILGenerator il,
        string field,
        Type fieldType,
        Type requestedType)
    {
        il.Emit(
            Ldstr,
            $"Cannot access field '{field}' because its type '{fieldType}' is not compatible with type '");
        il.Emit(Ldtoken, requestedType);
        il.Emit(Call, TypeGetTypeFromHandleMethod);
        il.Emit(Callvirt, typeof(object).GetMethod(nameof(ToString), Type.EmptyTypes)!);
        il.Emit(Ldstr, "'.");
        il.Emit(Call, StringConcat3Method);
        il.Emit(Newobj, InvalidOperationExceptionCtor);
        il.Emit(Throw);
    }
    
    public static void EmitThrowFieldTypeNotCompatibleException(
        ILGenerator il,
        int nameArg,
        Type type)
    {
        il.Emit( Ldstr, "Cannot access field '");
        il.Emit(Ldarg, nameArg);
        il.Emit(Ldstr, $"' because its type is not compatible with type '{type}'.");
        il.Emit(Call, StringConcat3Method);
        il.Emit(Newobj, InvalidOperationExceptionCtor);
        il.Emit(Throw);
    }
    
    public static void EmitTypeFromHandle(ILGenerator il, Type type)
    {
        il.Emit(Ldtoken, type);
        il.Emit(Call, TypeGetTypeFromHandleMethod);
    }

    public static void EmitTypeEqualsT(ILGenerator il, Type genericT, Type type)
    {
        EmitTypeFromHandle(il, genericT);
        EmitTypeFromHandle(il, type);
        il.Emit(Call, TypeOpEqualityMethod);
    }
}
