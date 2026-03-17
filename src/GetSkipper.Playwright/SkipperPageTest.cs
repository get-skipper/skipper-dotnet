using GetSkipper.Core;
using GetSkipper.NUnit;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace GetSkipper.Playwright;

/// <summary>
/// Drop-in replacement for <see cref="PageTest"/> that checks Skipper before
/// Playwright launches the browser.
///
/// <para>Usage: inherit from <c>SkipperPageTest</c> instead of <c>PageTest</c>:</para>
/// <code>
/// [assembly: SkipperConfig(SpreadsheetId = "1abc...", CredentialsFile = "service-account.json")]
///
/// [TestFixture]
/// public class LoginTests : SkipperPageTest   // was: PageTest
/// {
///     [Test]
///     public async Task UserCanLogin()
///     {
///         await Page.GotoAsync("/login");
///         // automatic skip is handled before this line runs
///     }
/// }
/// </code>
/// </summary>
public abstract class SkipperPageTest : PageTest
{
    [SetUp]
    public void SkipperBeforeTest()
    {
        CheckSkipper();
    }

    private void CheckSkipper()
    {
        if (!SkipperState.IsResolverSet) return;

        var resolver = SkipperState.Resolver;
        var testId = BuildTestId();
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

    private string BuildTestId()
    {
        var type = GetType();
        var ns = type.Namespace?.Replace('.', '/') ?? string.Empty;
        var className = type.Name;
        var methodName = TestContext.CurrentContext.Test.MethodName ?? string.Empty;

        var filePath = string.IsNullOrEmpty(ns)
            ? $"{className}.cs"
            : $"{ns}/{className}.cs";

        return TestIdHelper.Build(filePath, [className, methodName]);
    }
}
