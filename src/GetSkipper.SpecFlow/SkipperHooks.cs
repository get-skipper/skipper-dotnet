using GetSkipper.Core;
using NUnit.Framework;
using Reqnroll;

namespace GetSkipper.SpecFlow;

/// <summary>
/// Reqnroll binding class that hooks into the test run lifecycle to gate scenarios
/// against the Skipper spreadsheet.
///
/// <para>
/// This class is discovered automatically by Reqnroll because of the <c>[Binding]</c>
/// attribute and the package being referenced. <strong>No changes to step definitions
/// or feature files are required.</strong>
/// </para>
///
/// <para>
/// Configure via environment variables before running tests:
/// </para>
/// <code>
/// SKIPPER_SPREADSHEET_ID=1abc...
/// SKIPPER_CREDENTIALS_FILE=./service-account.json   # or SKIPPER_CREDENTIALS_BASE64
/// </code>
///
/// <para>Test ID format: <c>Features/Auth/Login.feature &gt; User authentication &gt; User can log in</c></para>
/// </summary>
[Binding]
public sealed class SkipperHooks(FeatureContext featureContext, ScenarioContext scenarioContext)
{
    private static SkipperResolver? _resolver;
    private static readonly List<string> _discoveredIds = [];
    private static readonly Lock _lock = new();

    [BeforeTestRun]
    public static async Task BeforeTestRunAsync()
    {
        Core.SkipperConfig config;
        try
        {
            config = SkipperConfigFactory.FromEnvironment();
        }
        catch (InvalidOperationException ex)
        {
            SkipperLogger.Warn($"Skipper not configured: {ex.Message} — all scenarios will run.");
            return;
        }

        var resolver = new SkipperResolver(config);
        await resolver.InitializeAsync();
        _resolver = resolver;
        SkipperLogger.Log("Skipper resolver ready (Reqnroll).");
    }

    [BeforeScenario(Order = int.MinValue)] // run before any other BeforeScenario hooks
    public void BeforeScenario()
    {
        if (_resolver is null) return;

        var featureTitle = featureContext.FeatureInfo.Title;
        var scenarioTitle = scenarioContext.ScenarioInfo.Title;

        // Derive the feature file path from the feature title
        // Reqnroll doesn't expose the .feature file path at runtime; we derive a
        // consistent pseudo-path from the namespace + feature folder info.
        var featureFilePath = DeriveFeatureFilePath(featureContext);

        var testId = TestIdHelper.Build(featureFilePath, [featureTitle, scenarioTitle]);

        lock (_lock) { _discoveredIds.Add(testId); }

        if (!_resolver.IsTestEnabled(testId))
        {
            var disabledUntil = _resolver.GetDisabledUntil(testId);
            var reason = disabledUntil.HasValue
                ? $"Disabled by Skipper until {disabledUntil:yyyy-MM-dd}"
                : "Disabled by Skipper";

            SkipperLogger.Log($"Skipping \"{testId}\" — {reason}");

            // Mark the scenario as inconclusive / pending — Reqnroll + NUnit shows it as "Ignored"
            scenarioContext.Pending();
        }
    }

    [AfterTestRun]
    public static async Task AfterTestRunAsync()
    {
        if (_resolver is null) return;
        if (_resolver.GetMode() != SkipperMode.Sync) return;

        SkipperLogger.Log("Syncing spreadsheet (Reqnroll)...");
        List<string> discovered;
        lock (_lock) { discovered = [.. _discoveredIds]; }

        var writer = _resolver.GetWriter();
        await writer.SyncAsync(discovered);
        SkipperLogger.Log("Sync complete.");
    }

    private static string DeriveFeatureFilePath(FeatureContext ctx)
    {
        // Reqnroll does not expose the .feature file path at runtime.
        // We construct a consistent pseudo-path from the feature folder tags or title.
        // Format: "Features/<FeatureTitle>.feature" (normalised, lowercase with dashes)
        var title = ctx.FeatureInfo.Title;
        var slug = title.ToLowerInvariant().Replace(' ', '-');
        return $"Features/{slug}.feature";
    }
}
