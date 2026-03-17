using GetSkipper.XUnit;
using Xunit;

namespace GetSkipper.Tests.XUnit;

public sealed class SkipperConfigBuilderTests
{
    [Fact]
    public void Build_ThrowsWhenAttributeMissing()
    {
        var assembly = typeof(string).Assembly;
        Assert.Throws<InvalidOperationException>(() => SkipperConfigBuilder.Build(assembly));
    }
}
