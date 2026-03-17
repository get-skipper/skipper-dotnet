using GetSkipper.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GetSkipper.MSTest.Tests;

[TestClass]
public sealed class SkipperGlobalHooksTests
{
    [TestMethod]
    public void BeforeTest_DoesNothingWhenNotInitialized()
    {
        // Skipper is not configured in this test project → BeforeTest should be a no-op
        var ctx = CreateTestContext();
        // Should not throw
        SkipperGlobalHooks.BeforeTest(ctx);
    }

    private static TestContext CreateTestContext()
    {
        // MSTest TestContext cannot be directly instantiated — we test the guard branch
        // by ensuring IsInitialized is false, which causes an early return.
        // The actual skip-path is covered by integration tests.
        return null!; // BeforeTest checks IsInitialized first, so null context is safe here
    }
}
