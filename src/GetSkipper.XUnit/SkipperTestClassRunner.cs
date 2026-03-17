using Xunit.Abstractions;
using Xunit.Sdk;

namespace GetSkipper.XUnit;

/// <summary>
/// Overrides the default xUnit test class runner to use <see cref="SkipperTestMethodRunner"/>.
/// </summary>
internal sealed class SkipperTestClassRunner(
    ITestClass testClass,
    IReflectionTypeInfo @class,
    IEnumerable<IXunitTestCase> testCases,
    IMessageSink diagnosticMessageSink,
    IMessageBus messageBus,
    ITestCaseOrderer testCaseOrderer,
    ExceptionAggregator aggregator,
    CancellationTokenSource cancellationTokenSource,
    IDictionary<Type, object> collectionFixtureMappings)
    : XunitTestClassRunner(
        testClass, @class, testCases, diagnosticMessageSink, messageBus,
        testCaseOrderer, aggregator, cancellationTokenSource, collectionFixtureMappings)
{
    protected override Task<RunSummary> RunTestMethodAsync(
        ITestMethod testMethod,
        IReflectionMethodInfo method,
        IEnumerable<IXunitTestCase> testCases,
        object?[] constructorArguments) =>
        new SkipperTestMethodRunner(
            testMethod,
            Class,
            method,
            testCases,
            DiagnosticMessageSink,
            MessageBus,
            new ExceptionAggregator(Aggregator),
            CancellationTokenSource,
            constructorArguments).RunAsync();
}
