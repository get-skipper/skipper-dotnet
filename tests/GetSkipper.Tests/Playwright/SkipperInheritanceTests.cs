using GetSkipper.Playwright;
using Xunit;

namespace GetSkipper.Tests.Playwright;

public sealed class PlaywrightSkipperInheritanceTests
{
    [Fact]
    public void SkipperPageTest_IsSubclassOfPageTest() =>
        Assert.Equal("PageTest", typeof(SkipperPageTest).BaseType?.Name);

    [Fact]
    public void SkipperBrowserTest_IsSubclassOfBrowserTest() =>
        Assert.Equal("BrowserTest", typeof(SkipperBrowserTest).BaseType?.Name);

    [Fact]
    public void SkipperContextTest_IsSubclassOfContextTest() =>
        Assert.Equal("ContextTest", typeof(SkipperContextTest).BaseType?.Name);
}
