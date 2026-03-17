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
               testCase.TestMethodArguments ?? [], messageBus, aggregator, cancellationTokenSource)
    {
    }

    protected override async Task<RunSummary> RunTestAsync()
    {
        if (!SkipperState.IsResolverSet)
            return await base.RunTestAsync();

        var resolver = SkipperState.Resolver;

        // Build the test ID: prefer source file from SourceInformation (populated when
        // PDB-backed discovery is active); fall back to a namespace-derived pseudo-path
        // (same convention used by the NUnit and MSTest integrations).
        var sourceInfo = TestCase.SourceInformation;
        var fqClassName = TestCase.TestMethod.TestClass.Class.Name;
        var methodName = TestCase.TestMethod.Method.Name;

        string filePath;
        if (!string.IsNullOrEmpty(sourceInfo?.FileName))
        {
            filePath = sourceInfo.FileName;
        }
        else
        {
            // Derive "Namespace/SubNamespace/ClassName.cs" from the fully-qualified class name
            var lastDot = fqClassName.LastIndexOf('.');
            var ns = lastDot > 0 ? fqClassName[..lastDot] : string.Empty;
            var simpleClass = lastDot > 0 ? fqClassName[(lastDot + 1)..] : fqClassName;
            var nsPath = ns.Replace('.', '/');
            filePath = string.IsNullOrEmpty(nsPath) ? $"{simpleClass}.cs" : $"{nsPath}/{simpleClass}.cs";
        }

        // Use only the simple class name in the title path (namespace is already in the file path)
        var lastDotForTitle = fqClassName.LastIndexOf('.');
        var className = lastDotForTitle > 0 ? fqClassName[(lastDotForTitle + 1)..] : fqClassName;

        var testId = TestIdHelper.Build(filePath, [className, methodName]);
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
