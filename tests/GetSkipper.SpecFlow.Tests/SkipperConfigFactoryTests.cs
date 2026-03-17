using GetSkipper.SpecFlow;
using NUnit.Framework;

namespace GetSkipper.SpecFlow.Tests;

[TestFixture]
public sealed class SkipperConfigFactoryTests
{
    [Test]
    public void FromEnvironment_ThrowsWhenSpreadsheetIdMissing()
    {
        Environment.SetEnvironmentVariable("SKIPPER_SPREADSHEET_ID", null);
        var ex = Assert.Throws<InvalidOperationException>(() => SkipperConfigFactory.FromEnvironment());
        Assert.That(ex!.Message, Does.Contain("SKIPPER_SPREADSHEET_ID"));
    }

    [Test]
    public void FromEnvironment_ThrowsWhenCredentialsMissing()
    {
        Environment.SetEnvironmentVariable("SKIPPER_SPREADSHEET_ID", "test-id");
        Environment.SetEnvironmentVariable("SKIPPER_CREDENTIALS_FILE", null);
        Environment.SetEnvironmentVariable("SKIPPER_CREDENTIALS_BASE64", null);

        var ex = Assert.Throws<InvalidOperationException>(() => SkipperConfigFactory.FromEnvironment());
        Assert.That(ex!.Message, Does.Contain("SKIPPER_CREDENTIALS"));

        Environment.SetEnvironmentVariable("SKIPPER_SPREADSHEET_ID", null);
    }
}
