using System;
using System.Collections.Generic;
using EasyTypeFactory.Utils;
using Xunit;

namespace EasyTypeFactory.UnitTests.Utils;

public class HashCodeExtensionsTests
{
    [Fact]
    public void AddCollection_WithNull_DoesNotThrow()
    {
        var hashCode = new HashCode();
        hashCode.AddCollection<string>(null);
        var initialHash = new HashCode().ToHashCode();
        Assert.Equal(initialHash, hashCode.ToHashCode());
    }

    [Fact]
    public void AddCollection_WithEmptyCollection_LeavesHashUnchanged()
    {
        var hashCode = new HashCode();
        hashCode.AddCollection(Array.Empty<string>());
        var initialHash = new HashCode().ToHashCode();
        Assert.Equal(initialHash, hashCode.ToHashCode());
    }

    [Fact]
    public void AddCollection_WithItems_ComputesConsistentHash()
    {
        var hash1 = new HashCode();
        hash1.AddCollection<string>(["alpha", "beta", "gamma"]);

        var hash2 = new HashCode();
        hash2.AddCollection<string>(["alpha", "beta", "gamma"]);

        var hash3 = new HashCode();
        hash3.AddCollection<string>(["gamma", "beta", "alpha"]);

        Assert.Equal(hash1.ToHashCode(), hash2.ToHashCode());
        Assert.NotEqual(hash1.ToHashCode(), hash3.ToHashCode());
    }

    [Fact]
    public void AddDictionary_WithNull_DoesNotThrow()
    {
        var hashCode = new HashCode();
        hashCode.AddDictionary<string, int>(null);
        var initialHash = new HashCode().ToHashCode();
        Assert.Equal(initialHash, hashCode.ToHashCode());
    }

    [Fact]
    public void AddDictionary_WithEmptyDictionary_LeavesHashUnchanged()
    {
        var hashCode = new HashCode();
        hashCode.AddDictionary(new Dictionary<string, int>());
        var initialHash = new HashCode().ToHashCode();
        Assert.Equal(initialHash, hashCode.ToHashCode());
    }

    [Fact]
    public void AddDictionary_WithItems_ComputesConsistentHash()
    {
        var dict1 = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
        var dict2 = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
        var dict3 = new Dictionary<string, int> { ["a"] = 2, ["b"] = 1 };

        var hash1 = new HashCode();
        hash1.AddDictionary(dict1);

        var hash2 = new HashCode();
        hash2.AddDictionary(dict2);

        var hash3 = new HashCode();
        hash3.AddDictionary(dict3);

        Assert.Equal(hash1.ToHashCode(), hash2.ToHashCode());
        Assert.NotEqual(hash1.ToHashCode(), hash3.ToHashCode());
    }
}
