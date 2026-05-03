using GetSkipper.Core;
using System.Text.Json;
using Xunit;

namespace GetSkipper.Tests.Core;

public class SkipperReporterTests
{
    [Fact]
    public void GenerateReport_WithNoSuppressedTests_ReturnsZeros()
    {
        // Arrange: create resolver with empty cache
        var config = new SkipperConfig
        {
            SpreadsheetId = "test-id",
            Credentials = null!, // Not used for this test
        };
        var resolver = SkipperResolver.FromJson("{}", config);
        var reporter = new SkipperReporter(resolver);

        // Act
        var report = reporter.GenerateReport();

        // Assert
        Assert.Equal(0, report.TotalSuppressed);
        Assert.Equal(0, report.ExpiringThisWeek);
        Assert.Equal(0, report.QuarantineDaysOfDebt);
    }

    [Fact]
    public void GenerateReport_WithSuppressedTests_CountsCorrectly()
    {
        // Arrange: resolver with suppressed tests
        var tomorrow = DateTimeOffset.UtcNow.AddDays(1).ToString("O");
        var nextWeek = DateTimeOffset.UtcNow.AddDays(5).ToString("O");
        var pastDate = DateTimeOffset.UtcNow.AddDays(-1).ToString("O");

        var cache = new Dictionary<string, string?>
        {
            { "TestA", nextWeek },         // expires in 5 days (suppressed, expiring this week)
            { "TestB", tomorrow },         // expires tomorrow (suppressed, expiring this week)
            { "TestC", pastDate },         // already expired (not suppressed)
            { "TestD", null },             // no date (not suppressed)
        };

        var json = JsonSerializer.Serialize(cache);
        var config = new SkipperConfig
        {
            SpreadsheetId = "test-id",
            Credentials = null!,
        };
        var resolver = SkipperResolver.FromJson(json, config);
        var reporter = new SkipperReporter(resolver);

        // Act
        var report = reporter.GenerateReport();

        // Assert
        Assert.Equal(2, report.TotalSuppressed);        // TestA, TestB
        Assert.Equal(2, report.ExpiringThisWeek);       // TestA, TestB
        Assert.True(report.QuarantineDaysOfDebt > 0);   // Sum of days
        Assert.NotEmpty(report.SuppressedTests);
    }

    [Fact]
    public void GenerateMarkdownSummary_IncludesMetrics()
    {
        // Arrange
        var report = new QuarantineReport
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            TotalSuppressed = 5,
            ExpiringThisWeek = 2,
            ReenabledThisRun = 1,
            QuarantineDaysOfDebt = 10,
            SuppressedTests = new()
            {
                new TestMetric
                {
                    TestId = "TestA",
                    DisabledUntil = DateTimeOffset.UtcNow.AddDays(3)
                }
            },
            ReenabledTests = new() { "OldTestA" }
        };

        var config = new SkipperConfig
        {
            SpreadsheetId = "test-id",
            Credentials = null!,
        };
        var resolver = SkipperResolver.FromJson("{}", config);
        var reporter = new SkipperReporter(resolver);

        // Act
        var markdown = reporter.GenerateMarkdownSummary(report);

        // Assert
        Assert.Contains("5", markdown);        // TotalSuppressed
        Assert.Contains("2", markdown);        // ExpiringThisWeek
        Assert.Contains("1", markdown);        // ReenabledThisRun
        Assert.Contains("10", markdown);       // QuarantineDaysOfDebt
        Assert.Contains("TestA", markdown);    // Test in expiring list
        Assert.Contains("OldTestA", markdown); // Test in reenabled list
    }

    [Fact]
    public void WriteReportJson_CreatesFile()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test-report-{Guid.NewGuid()}.json");
        var report = new QuarantineReport
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            TotalSuppressed = 3,
            ExpiringThisWeek = 1,
            QuarantineDaysOfDebt = 7,
            SuppressedTests = new()
        };

        var config = new SkipperConfig
        {
            SpreadsheetId = "test-id",
            Credentials = null!,
        };
        var resolver = SkipperResolver.FromJson("{}", config);
        var reporter = new SkipperReporter(resolver);

        try
        {
            // Act
            reporter.WriteReportJson(report, tempFile);

            // Assert
            Assert.True(File.Exists(tempFile));

            var json = File.ReadAllText(tempFile);
            var parsed = JsonSerializer.Deserialize<QuarantineReport>(json);
            Assert.NotNull(parsed);
            Assert.Equal(3, parsed.TotalSuppressed);
            Assert.Equal(1, parsed.ExpiringThisWeek);
            Assert.Equal(7, parsed.QuarantineDaysOfDebt);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void GenerateReport_TracksReenabledTests()
    {
        // Arrange: create a previous report with suppressed tests
        var previousReportFile = Path.Combine(Path.GetTempPath(), $"prev-report-{Guid.NewGuid()}.json");
        var nextWeek = DateTimeOffset.UtcNow.AddDays(5).ToString("O");

        var previousReport = new QuarantineReport
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            TotalSuppressed = 2,
            ExpiringThisWeek = 0,
            ReenabledThisRun = 0,
            QuarantineDaysOfDebt = 10,
            SuppressedTests = new()
            {
                new TestMetric { TestId = "TestA", DisabledUntil = DateTimeOffset.UtcNow.AddDays(5) },
                new TestMetric { TestId = "TestB", DisabledUntil = DateTimeOffset.UtcNow.AddDays(3) }
            },
            ReenabledTests = new()
        };

        var previousJson = JsonSerializer.Serialize(previousReport);
        File.WriteAllText(previousReportFile, previousJson);

        // Now create a current cache where TestB is re-enabled (no longer in the list)
        var currentCache = new Dictionary<string, string?>
        {
            { "TestA", nextWeek }  // Still suppressed
            // TestB is gone, so it's re-enabled
        };

        var cacheJson = JsonSerializer.Serialize(currentCache);
        var config = new SkipperConfig
        {
            SpreadsheetId = "test-id",
            Credentials = null!,
        };
        var resolver = SkipperResolver.FromJson(cacheJson, config);
        var reporter = new SkipperReporter(resolver);

        try
        {
            // Act
            var report = reporter.GenerateReport(previousReportFile);

            // Assert
            Assert.Equal(1, report.TotalSuppressed);      // Only TestA now
            Assert.Equal(1, report.ReenabledThisRun);     // TestB was re-enabled
            Assert.Contains("TestB", report.ReenabledTests);
        }
        finally
        {
            if (File.Exists(previousReportFile))
                File.Delete(previousReportFile);
        }
    }
}
