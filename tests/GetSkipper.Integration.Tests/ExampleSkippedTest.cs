using Xunit;

namespace GetSkipper.Integration.Tests;

/// <summary>
/// Self-test: this project uses GetSkipper.XUnit on itself.
/// If "skipper-dotnet integration test" is present in the spreadsheet with a future date,
/// the test below will be skipped automatically.
///
/// Run with:
///   SKIPPER_SPREADSHEET_ID=... GOOGLE_CREDS_B64=... dotnet test tests/GetSkipper.Integration.Tests
/// </summary>
public sealed class ExampleTests
{
    [Fact]
    public void AlwaysPassingTest()
    {
        // This test should always pass — it verifies the infrastructure works.
        Assert.True(true);
    }

    [Fact]
    public void CanBeDisabledViaSpreadsheet()
    {
        // Add a row to the spreadsheet with:
        //   testId: "tests/GetSkipper.Integration.Tests/ExampleSkippedTest.cs > ExampleTests > CanBeDisabledViaSpreadsheet"
        //   disabledUntil: 2099-01-01
        // to make this test be skipped automatically.
        Assert.True(true);
    }
}
