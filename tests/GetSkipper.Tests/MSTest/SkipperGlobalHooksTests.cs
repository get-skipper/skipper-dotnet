using GetSkipper.MSTest;
using Xunit;

namespace GetSkipper.Tests.MSTest;

public sealed class MSTestSkipperGlobalHooksTests
{
    [Fact]
    public void BeforeTest_DoesNothingWhenNotInitialized()
    {
        // Skipper is not configured → BeforeTest should be a no-op (IsInitialized == false)
        SkipperGlobalHooks.BeforeTest(null!);
    }
}
