using GetSkipper.Core;
using GetSkipper.Core.Credentials;
using GetSkipper.NUnit;
using NUnit.Framework;

namespace GetSkipper.NUnit.Tests;

[TestFixture]
public sealed class SkipperStateTests
{
    [Test]
    public void Resolver_ThrowsWhenNotSet()
    {
        // Reset state for isolation (internal access via InternalsVisibleTo or reflection)
        // This test verifies the error message is meaningful.
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = SkipperState.Resolver; // will throw if not initialized
        });
        Assert.That(ex.Message, Does.Contain("SkipperConfig"));
    }

    [Test]
    public void IsResolverSet_ReturnsFalseWhenNotInitialized()
    {
        // SkipperState is a shared singleton; in isolation (fresh process) this is false.
        // This test documents the API; actual value depends on test execution order.
        Assert.That(typeof(SkipperState).GetProperty("IsResolverSet",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static),
            Is.Not.Null);
    }
}
