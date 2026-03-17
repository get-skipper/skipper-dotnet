using GetSkipper.Core;
using GetSkipper.NUnit;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace GetSkipper.Playwright;

/// <summary>
/// Drop-in replacement for <see cref="BrowserTest"/> that checks Skipper before
/// Playwright launches the browser.
/// </summary>
public abstract class SkipperBrowserTest : BrowserTest
{
    [SetUp]
    public void SkipperBeforeTest()
    {
        if (!SkipperState.IsResolverSet) return;

        var resolver = SkipperState.Resolver;
        var type = GetType();
        var ns = type.Namespace?.Replace('.', '/') ?? string.Empty;
        var className = type.Name;
        var methodName = TestContext.CurrentContext.Test.MethodName ?? string.Empty;

        var filePath = string.IsNullOrEmpty(ns)
            ? $"{className}.cs"
            : $"{ns}/{className}.cs";

        var testId = TestIdHelper.Build(filePath, [className, methodName]);
        SkipperState.RecordDiscoveredId(testId);

        if (!resolver.IsTestEnabled(testId))
        {
            var disabledUntil = resolver.GetDisabledUntil(testId);
            var reason = disabledUntil.HasValue
                ? $"Disabled by Skipper until {disabledUntil:yyyy-MM-dd}"
                : "Disabled by Skipper";

            SkipperLogger.Log($"Skipping \"{testId}\" — {reason}");
            Assert.Ignore(reason);
        }
    }
}
