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

        var testId = BuildTestId(TestCase.TestMethod.TestClass.Class.Name, TestCase.TestMethod.Method.Name);
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

    /// <summary>
    /// Builds "Namespace/Sub/ClassName.cs > ClassName > Method", the same convention used by the
    /// NUnit and MSTest integrations. <c>SourceInformation.FileName</c> is deliberately ignored:
    /// it is an absolute, machine-specific PDB path, so IDs would differ between machines.
    /// </summary>
    internal static string BuildTestId(string fqClassName, string methodName)
    {
        var lastDot = fqClassName.LastIndexOf('.');
        var ns = lastDot > 0 ? fqClassName[..lastDot] : string.Empty;
        var className = lastDot > 0 ? fqClassName[(lastDot + 1)..] : fqClassName;
        var nsPath = ns.Replace('.', '/');
        var filePath = string.IsNullOrEmpty(nsPath) ? $"{className}.cs" : $"{nsPath}/{className}.cs";
        return TestIdHelper.Build(filePath, [className, methodName]);
    }
}
