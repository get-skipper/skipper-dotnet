using Xunit.Abstractions;
using Xunit.Sdk;

namespace GetSkipper.XUnit;

/// <summary>
/// Overrides the default xUnit test collection runner to use <see cref="SkipperTestClassRunner"/>.
/// </summary>
internal sealed class SkipperTestCollectionRunner(
    ITestCollection testCollection,
    IEnumerable<IXunitTestCase> testCases,
    IMessageSink diagnosticMessageSink,
    IMessageBus messageBus,
    ITestCaseOrderer testCaseOrderer,
    ExceptionAggregator aggregator,
    CancellationTokenSource cancellationTokenSource)
    : XunitTestCollectionRunner(
        testCollection, testCases, diagnosticMessageSink, messageBus,
        testCaseOrderer, aggregator, cancellationTokenSource)
{
    protected override Task<RunSummary> RunTestClassAsync(
        ITestClass testClass,
        IReflectionTypeInfo @class,
        IEnumerable<IXunitTestCase> testCases) =>
        new SkipperTestClassRunner(
            testClass,
            @class,
            testCases,
            DiagnosticMessageSink,
            MessageBus,
            TestCaseOrderer,
            new ExceptionAggregator(Aggregator),
            CancellationTokenSource,
            CollectionFixtureMappings).RunAsync();
}
