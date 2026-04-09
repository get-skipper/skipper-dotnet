using GetSkipper.Core;
using Xunit;

namespace GetSkipper.Tests.Core;

public sealed class SheetsClientDateParsingTests
{
    [Fact]
    public void ParseDisabledUntil_ValidDate_ReturnsMidnightNextDayUtc()
    {
        var result = SheetsClient.ParseDisabledUntil("2026-04-01", 2, "Sheet1");

        Assert.Equal(new DateTimeOffset(2026, 4, 2, 0, 0, 0, TimeSpan.Zero), result);
    }

    [Theory]
    [InlineData("2026-4-1")]
    [InlineData("01/04/2026")]
    [InlineData("April 1, 2026")]
    [InlineData("2026/04/01")]
    [InlineData("not-a-date")]
    public void ParseDisabledUntil_InvalidFormat_Throws(string bad)
    {
        var ex = Assert.Throws<ArgumentException>(
            () => SheetsClient.ParseDisabledUntil(bad, 3, "Sheet1"));
        Assert.Contains("YYYY-MM-DD", ex.Message);
        Assert.Contains("Row 3", ex.Message);
    }

    [Fact]
    public void ParseDisabledUntil_TimezoneConsistency_SameInstantRegardlessOfLocalTz()
    {
        // Whatever the OS timezone, the expiry must be 2026-04-02T00:00:00Z.
        var result = SheetsClient.ParseDisabledUntil("2026-04-01", 1, "Sheet1");

        Assert.Equal(DateTimeKind.Unspecified, result.DateTime.Kind);
        Assert.Equal(TimeSpan.Zero, result.Offset);
        Assert.Equal(new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc), result.UtcDateTime);
    }

    [Fact]
    public void ParseDisabledUntil_DateInFuture_IsConsideredDisabled()
    {
        var futureDate = DateTimeOffset.UtcNow.AddDays(10).ToString("yyyy-MM-dd");
        var until = SheetsClient.ParseDisabledUntil(futureDate, 1, "Sheet1");

        Assert.True(DateTimeOffset.UtcNow < until);
    }

    [Fact]
    public void ParseDisabledUntil_DateInPast_IsConsideredEnabled()
    {
        var pastDate = DateTimeOffset.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
        var until = SheetsClient.ParseDisabledUntil(pastDate, 1, "Sheet1");

        Assert.True(DateTimeOffset.UtcNow >= until);
    }
}
