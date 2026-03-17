using GetSkipper.SpecFlow;
using Xunit;

namespace GetSkipper.Tests.SpecFlow;

public sealed class SpecFlowSkipperConfigFactoryTests
{
    [Fact]
    public void FromEnvironment_ThrowsWhenSpreadsheetIdMissing()
    {
        Environment.SetEnvironmentVariable("SKIPPER_SPREADSHEET_ID", null);
        var ex = Assert.Throws<InvalidOperationException>(() => SkipperConfigFactory.FromEnvironment());
        Assert.Contains("SKIPPER_SPREADSHEET_ID", ex.Message);
    }

    [Fact]
    public void FromEnvironment_ThrowsWhenCredentialsMissing()
    {
        Environment.SetEnvironmentVariable("SKIPPER_SPREADSHEET_ID", "test-id");
        Environment.SetEnvironmentVariable("SKIPPER_CREDENTIALS_FILE", null);
        Environment.SetEnvironmentVariable("SKIPPER_CREDENTIALS_BASE64", null);

        var ex = Assert.Throws<InvalidOperationException>(() => SkipperConfigFactory.FromEnvironment());
        Assert.Contains("SKIPPER_CREDENTIALS", ex.Message);

        Environment.SetEnvironmentVariable("SKIPPER_SPREADSHEET_ID", null);
    }
}
