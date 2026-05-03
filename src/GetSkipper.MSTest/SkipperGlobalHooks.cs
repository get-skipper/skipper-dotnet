using GetSkipper.Core;
using GetSkipper.Core.Credentials;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GetSkipper.MSTest;

/// <summary>
/// Provides the <c>[GlobalTestInitialize]</c> and <c>[GlobalTestCleanup]</c> hooks
/// that MSTest v3.10+ runs before and after every test in the assembly.
///
/// <para>Call <see cref="ConfigureAsync"/> once from your own
/// <c>[GlobalTestInitialize]</c> method:</para>
/// <code>
/// [GlobalTestInitialize]
/// public static Task SkipperInit()
///     => SkipperGlobalHooks.ConfigureAsync(new SkipperConfig
///     {
///         SpreadsheetId = "1abc...",
///         Credentials = new FileCredentials("service-account.json"),
///     });
/// </code>
/// </summary>
public static class SkipperGlobalHooks
{
    /// <summary>
    /// Initialises the Skipper resolver with the given <paramref name="config"/>.
    /// Call this from your own <c>[GlobalTestInitialize]</c> method.
    /// </summary>
    public static async Task ConfigureAsync(SkipperConfig config, CancellationToken ct = default)
    {
        var resolver = new SkipperResolver(config);
        await resolver.InitializeAsync(ct);
        SkipperState.Resolver = resolver;
        SkipperLogger.Log("Skipper resolver ready (MSTest).");
    }

    /// <summary>
    /// Checks whether <paramref name="testContext"/> should be skipped.
    /// Call this from your own <c>[GlobalTestInitialize]</c> method (after
    /// <see cref="ConfigureAsync"/> has been called at assembly scope).
    /// </summary>
    /// <remarks>
    /// Because MSTest's <c>[GlobalTestInitialize]</c> runs for every test,
    /// a single method can do both: configure (if not yet done) and skip-check.
    /// </remarks>
    public static void BeforeTest(TestContext testContext)
    {
        if (!SkipperState.IsInitialized) return;

        var resolver = SkipperState.Resolver!;

        // Derive a pseudo-path from the fully qualified test name
        // MSTest provides "ClassName.MethodName" in TestContext.FullyQualifiedTestClassName
        var fqName = testContext.FullyQualifiedTestClassName ?? string.Empty;
        var methodName = testContext.TestName ?? string.Empty;

        // Convert "Namespace.ClassName" → "Namespace/ClassName.cs"
        var parts = fqName.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var className = parts.LastOrDefault() ?? fqName;
        var namespacePath = parts.Length > 1
            ? string.Join("/", parts[..^1])
            : string.Empty;

        var filePath = string.IsNullOrEmpty(namespacePath)
            ? $"{className}.cs"
            : $"{namespacePath}/{className}.cs";

        var testId = TestIdHelper.Build(filePath, [className, methodName]);
        SkipperState.RecordDiscoveredId(testId);

        if (!resolver.IsTestEnabled(testId))
        {
            var disabledUntil = resolver.GetDisabledUntil(testId);
            var reason = disabledUntil.HasValue
                ? $"Disabled by Skipper until {disabledUntil:yyyy-MM-dd}"
                : "Disabled by Skipper";

            SkipperLogger.Log($"Skipping \"{testId}\" — {reason}");
            Assert.Inconclusive(reason);
        }
    }

    /// <summary>
    /// Syncs the spreadsheet (only in <c>SKIPPER_MODE=sync</c>) and generates the quarantine report.
    /// Call this from your own <c>[GlobalTestCleanup]</c> method.
    /// </summary>
    public static async Task AfterAllTestsAsync(CancellationToken ct = default)
    {
        if (!SkipperState.IsInitialized) return;

        var resolver = SkipperState.Resolver!;

        if (resolver.GetMode() == SkipperMode.Sync)
        {
            SkipperLogger.Log("Syncing spreadsheet (MSTest)...");
            var writer = resolver.GetWriter();
            await writer.SyncAsync(SkipperState.GetDiscoveredIds(), ct);
            SkipperLogger.Log("Sync complete.");
        }

        // Generate and write quarantine report
        var reporter = new SkipperReporter(resolver);
        reporter.ExecuteReport();
    }
}
