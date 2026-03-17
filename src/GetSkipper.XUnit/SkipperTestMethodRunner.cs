using Xunit.Abstractions;
using Xunit.Sdk;

namespace GetSkipper.XUnit;

/// <summary>
/// Overrides the default xUnit test method runner to use <see cref="SkipperTestCaseRunner"/>
/// for every test case, injecting the Skipper check transparently.
/// </summary>
internal sealed class SkipperTestMethodRunner(
    ITestMethod testMethod,
    IReflectionTypeInfo @class,
    IReflectionMethodInfo method,
    IEnumerable<IXunitTestCase> testCases,
    IMessageSink diagnosticMessageSink,
    IMessageBus messageBus,
    ExceptionAggregator aggregator,
    CancellationTokenSource cancellationTokenSource,
    object?[] constructorArguments)
    : XunitTestMethodRunner(
        testMethod, @class, method, testCases, diagnosticMessageSink,
        messageBus, aggregator, cancellationTokenSource, constructorArguments)
{
    protected override Task<RunSummary> RunTestCaseAsync(IXunitTestCase testCase) =>
        new SkipperTestCaseRunner(
            testCase,
            testCase.DisplayName,
            testCase.SkipReason,
            constructorArguments,
            diagnosticMessageSink,
            messageBus,
            new ExceptionAggregator(aggregator),
            cancellationTokenSource).RunAsync();
}
