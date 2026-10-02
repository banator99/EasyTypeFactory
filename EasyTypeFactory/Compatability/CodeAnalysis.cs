// ReSharper disable CheckNamespace

#if NETSTANDARD2_1

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Constructor)]
    internal sealed class SetsRequiredMembersAttribute : Attribute;
}

#endif