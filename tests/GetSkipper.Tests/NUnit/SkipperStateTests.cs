using GetSkipper.NUnit;
using Xunit;

namespace GetSkipper.Tests.NUnit;

public sealed class NUnitSkipperStateTests
{
    [Fact]
    public void Resolver_ThrowsWhenNotSet()
    {
        if (SkipperState.IsResolverSet) return; // already initialized by another test
        var ex = Assert.Throws<InvalidOperationException>(() => _ = SkipperState.Resolver);
        Assert.Contains("SkipperConfig", ex.Message);
    }

    [Fact]
    public void IsResolverSet_PropertyIsAccessible()
    {
        _ = SkipperState.IsResolverSet; // accessible via InternalsVisibleTo
    }
}
