using GetSkipper.XUnit;
using System.Reflection;
using Xunit;

namespace GetSkipper.XUnit.Tests;

public sealed class SkipperConfigBuilderTests
{
    [Fact]
    public void Build_ThrowsWhenAttributeMissing()
    {
        // Use an assembly that doesn't have [SkipperConfig]
        var assembly = typeof(string).Assembly;
        Assert.Throws<InvalidOperationException>(() => SkipperConfigBuilder.Build(assembly));
    }
}
