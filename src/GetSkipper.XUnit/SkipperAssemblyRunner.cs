using GetSkipper.Core;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace GetSkipper.XUnit;

/// <summary>
/// Overrides the default xUnit assembly runner.
/// Initialises the <see cref="SkipperResolver"/> before any tests run and
/// optionally syncs the spreadsheet after all tests complete.
/// </summary>
internal sealed class SkipperAssemblyRunner(
    ITestAssembly testAssembly,
    IEnumerable<IXunitTestCase> testCases,
    IMessageSink diagnosticMessageSink,
    IMessageSink executionMessageSink,
    ITestFrameworkExecutionOptions executionOptions)
    : XunitTestAssemblyRunner(
        testAssembly, testCases, diagnosticMessageSink,
        executionMessageSink, executionOptions)
{
    protected override async Task AfterTestAssemblyStartingAsync()
    {
        await base.AfterTestAssemblyStartingAsync();

        var assembly = ((IReflectionAssemblyInfo)TestAssembly.Assembly).Assembly;

        SkipperConfig config;
        try
        {
            config = SkipperConfigBuilder.Build(assembly);
        }
        catch (InvalidOperationException ex)
        {
            SkipperLogger.Warn($"Skipper not configured: {ex.Message} — all tests will run.");
            return;
        }

        var resolver = new SkipperResolver(config);
        SkipperLogger.Log("Initialising Skipper resolver...");
        await resolver.InitializeAsync();
        SkipperState.Resolver = resolver;
        SkipperLogger.Log("Skipper resolver ready.");
    }

    protected override async Task BeforeTestAssemblyFinishedAsync()
    {
        if (!SkipperState.IsResolverSet)
        {
            await base.BeforeTestAssemblyFinishedAsync();
            return;
        }

        var resolver = SkipperState.Resolver;

        if (resolver.GetMode() == SkipperMode.Sync)
        {
            SkipperLogger.Log("Syncing spreadsheet...");
            var discovered = SkipperState.GetDiscoveredIds();
            var writer = resolver.GetWriter();
            await writer.SyncAsync(discovered);
            SkipperLogger.Log("Sync complete.");
        }

        await base.BeforeTestAssemblyFinishedAsync();
    }

    protected override Task<RunSummary> RunTestCollectionAsync(
        IMessageBus messageBus,
        ITestCollection testCollection,
        IEnumerable<IXunitTestCase> testCases,
        CancellationTokenSource cancellationTokenSource) =>
        new SkipperTestCollectionRunner(
            testCollection,
            testCases,
            DiagnosticMessageSink,
            messageBus,
            TestCaseOrderer,
            new ExceptionAggregator(Aggregator),
            cancellationTokenSource).RunAsync();
}
