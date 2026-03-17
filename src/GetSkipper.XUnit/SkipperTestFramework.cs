using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace GetSkipper.XUnit;

/// <summary>
/// Custom xUnit v2 <see cref="XunitTestFramework"/> that injects the Skipper check
/// into every <c>[Fact]</c> and <c>[Theory]</c> test in the assembly.
///
/// <para>Register it once in your test project (e.g., <c>AssemblyInfo.cs</c>):</para>
/// <code>
/// [assembly: TestFramework("GetSkipper.XUnit.SkipperTestFramework", "GetSkipper.XUnit")]
/// [assembly: SkipperConfig(SpreadsheetId = "1abc...", CredentialsFile = "service-account.json")]
/// </code>
/// <para>No other changes to your tests are required.</para>
/// </summary>
public sealed class SkipperTestFramework(IMessageSink messageSink)
    : XunitTestFramework(messageSink)
{
    protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName) =>
        new SkipperExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
}

internal sealed class SkipperExecutor(
    AssemblyName assemblyName,
    ISourceInformationProvider sourceInformationProvider,
    IMessageSink diagnosticMessageSink)
    : XunitTestFrameworkExecutor(assemblyName, sourceInformationProvider, diagnosticMessageSink)
{
    protected override async void RunTestCases(
        IEnumerable<IXunitTestCase> testCases,
        IMessageSink executionMessageSink,
        ITestFrameworkExecutionOptions executionOptions)
    {
        using var runner = new SkipperAssemblyRunner(
            TestAssembly,
            testCases,
            DiagnosticMessageSink,
            executionMessageSink,
            executionOptions);

        await runner.RunAsync();
    }
}
