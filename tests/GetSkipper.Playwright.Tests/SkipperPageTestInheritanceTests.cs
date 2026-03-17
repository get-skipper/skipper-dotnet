using GetSkipper.Playwright;
using NUnit.Framework;

namespace GetSkipper.Playwright.Tests;

/// <summary>
/// Verifies that <see cref="SkipperPageTest"/> is a valid base class for Playwright tests.
/// </summary>
[TestFixture]
public sealed class SkipperPageTestInheritanceTests
{
    [Test]
    public void SkipperPageTest_IsSubclassOfPageTest()
    {
        Assert.That(typeof(SkipperPageTest).BaseType?.Name, Is.EqualTo("PageTest"));
    }

    [Test]
    public void SkipperBrowserTest_IsSubclassOfBrowserTest()
    {
        Assert.That(typeof(SkipperBrowserTest).BaseType?.Name, Is.EqualTo("BrowserTest"));
    }

    [Test]
    public void SkipperContextTest_IsSubclassOfContextTest()
    {
        Assert.That(typeof(SkipperContextTest).BaseType?.Name, Is.EqualTo("ContextTest"));
    }
}
