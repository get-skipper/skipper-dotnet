using GetSkipper.Core;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace GetSkipper.XUnit;

/// <summary>
/// Wraps the default xUnit test case runner to inject the Skipper skip check
/// before each test executes.
/// </summary>
internal sealed class SkipperTestCaseRunner : XunitTestCaseRunner
{
    public SkipperTestCaseRunner(
        IXunitTestCase testCase,
        string displayName,
        string? skipReason,
        object?[] constructorArguments,
        IMessageBus messageBus,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
        : base(testCase, displayName, skipReason, constructorArguments,
               [], messageBus, aggregator, cancellationTokenSource)
    {
    }

    protected override async Task<RunSummary> RunTestAsync()
    {
        if (!SkipperState.IsResolverSet)
            return await base.RunTestAsync();

        var resolver = SkipperState.Resolver;

        // Build the test ID from the test case source info
        var sourceInfo = TestCase.SourceInformation;
        var fileName = sourceInfo?.FileName ?? string.Empty;
        var className = TestCase.TestMethod.TestClass.Class.Name;
        var methodName = TestCase.TestMethod.Method.Name;

        var testId = TestIdHelper.Build(fileName, [className, methodName]);
        SkipperState.RecordDiscoveredId(testId);

        if (!resolver.IsTestEnabled(testId))
        {
            var disabledUntil = resolver.GetDisabledUntil(testId);
            var reason = disabledUntil.HasValue
                ? $"Disabled by Skipper until {disabledUntil:yyyy-MM-dd}"
                : "Disabled by Skipper";

            SkipperLogger.Log($"Skipping \"{testId}\" — {reason}");

            // Report as skipped by returning a summary with 1 skipped test
            var skippedSummary = new RunSummary { Total = 1, Skipped = 1 };
            var skippedMessage = new TestSkipped(
                new XunitTest(TestCase, DisplayName), reason);
            MessageBus.QueueMessage(skippedMessage);
            return skippedSummary;
        }

        return await base.RunTestAsync();
    }
}
