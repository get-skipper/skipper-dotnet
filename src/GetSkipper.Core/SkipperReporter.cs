using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GetSkipper.Core;

/// <summary>
/// Generates quarantine reports after test execution.
/// </summary>
public sealed class SkipperReporter
{
    private readonly SkipperResolver _resolver;

    public SkipperReporter(SkipperResolver resolver)
    {
        _resolver = resolver;
    }

    /// <summary>
    /// Generates a quarantine report with metrics about suppressed tests.
    /// Compares with the previous report (if available) to determine re-enabled tests.
    /// </summary>
    public QuarantineReport GenerateReport(string previousReportPath = "skipper-report.json")
    {
        var now = DateTimeOffset.UtcNow;
        var weekFromNow = now.AddDays(7);

        var suppressedTests = new List<TestMetric>();
        var expiringThisWeek = new List<TestMetric>();
        var reenabledTests = new List<string>();
        var totalQuarantineDays = 0;

        // Get the previous report's suppressed tests (if available)
        var previousSuppressed = GetPreviousSuppressedTests(previousReportPath);

        // Iterate through cache entries
        foreach (var entry in GetAllCacheEntries())
        {
            var disabled = entry.Value;
            if (disabled == null || !DateTimeOffset.TryParse(disabled, out var disabledUntil))
                continue;

            if (disabledUntil <= now)
                continue; // test is already re-enabled

            suppressedTests.Add(new TestMetric
            {
                TestId = entry.Key,
                DisabledUntil = disabledUntil
            });

            var daysRemaining = (int)(disabledUntil.Date - now.Date).TotalDays;
            totalQuarantineDays += daysRemaining;

            if (disabledUntil <= weekFromNow)
            {
                expiringThisWeek.Add(new TestMetric
                {
                    TestId = entry.Key,
                    DisabledUntil = disabledUntil
                });
            }
        }

        // Find tests that were suppressed before but are not now
        foreach (var previousTest in previousSuppressed)
        {
            var currentSuppressionKey = suppressedTests.FirstOrDefault(t =>
                t.TestId.Equals(previousTest, StringComparison.OrdinalIgnoreCase));

            if (currentSuppressionKey == null)
            {
                reenabledTests.Add(previousTest);
            }
        }

        return new QuarantineReport
        {
            GeneratedAtUtc = now,
            TotalSuppressed = suppressedTests.Count,
            ExpiringThisWeek = expiringThisWeek.Count,
            ReenabledThisRun = reenabledTests.Count,
            QuarantineDaysOfDebt = totalQuarantineDays,
            SuppressedTests = suppressedTests.OrderBy(t => t.DisabledUntil).ToList(),
            ReenabledTests = reenabledTests
        };
    }

    private IEnumerable<string> GetPreviousSuppressedTests(string reportPath)
    {
        try
        {
            if (!File.Exists(reportPath))
                return [];

            var json = File.ReadAllText(reportPath);
            var previousReport = JsonSerializer.Deserialize<QuarantineReport>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return previousReport?.SuppressedTests
                .Select(t => t.TestId)
                .ToList() ?? [];
        }
        catch (Exception ex)
        {
            SkipperLogger.Warn($"[skipper] Failed to read previous report: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Writes the report as JSON to <c>skipper-report.json</c> in the current working directory.
    /// </summary>
    public void WriteReportJson(QuarantineReport report, string filePath = "skipper-report.json")
    {
        try
        {
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            File.WriteAllText(filePath, json);
            SkipperLogger.Log($"[skipper] Report written to {filePath}");
        }
        catch (Exception ex)
        {
            SkipperLogger.Warn($"[skipper] Failed to write report JSON: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates a markdown summary suitable for GitHub Actions GITHUB_STEP_SUMMARY.
    /// If GITHUB_STEP_SUMMARY env var is set, writes to that file; otherwise returns the markdown.
    /// </summary>
    public string GenerateMarkdownSummary(QuarantineReport report)
    {
        var summary = new System.Text.StringBuilder();
        summary.AppendLine("# Quarantine Report");
        summary.AppendLine();
        summary.AppendLine($"- **Tests Currently Suppressed**: {report.TotalSuppressed}");
        summary.AppendLine($"- **Expiring This Week**: {report.ExpiringThisWeek}");

        if (report.ReenabledThisRun > 0)
        {
            summary.AppendLine($"- **Re-enabled This Run**: {report.ReenabledThisRun}");
        }

        summary.AppendLine($"- **Quarantine Days of Debt**: {report.QuarantineDaysOfDebt}");
        summary.AppendLine();

        if (report.ReenabledThisRun > 0)
        {
            summary.AppendLine("## Tests Re-enabled This Run");
            summary.AppendLine();
            foreach (var test in report.ReenabledTests)
            {
                summary.AppendLine($"- `{test}`");
            }
            summary.AppendLine();
        }

        if (report.ExpiringThisWeek > 0)
        {
            summary.AppendLine("## Tests Expiring This Week");
            summary.AppendLine();
            foreach (var test in report.SuppressedTests.Where(t => t.DisabledUntil <= DateTimeOffset.UtcNow.AddDays(7)))
            {
                var daysLeft = (int)(test.DisabledUntil.Date - DateTimeOffset.UtcNow.Date).TotalDays;
                summary.AppendLine($"- `{test.TestId}` — expires in {daysLeft} day(s) ({test.DisabledUntil:yyyy-MM-dd})");
            }
            summary.AppendLine();
        }

        return summary.ToString();
    }

    /// <summary>
    /// Writes the markdown summary to GITHUB_STEP_SUMMARY if available, otherwise logs it.
    /// </summary>
    public void WriteSummaryToGitHub(string markdown)
    {
        var githubStepSummary = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
        if (!string.IsNullOrEmpty(githubStepSummary))
        {
            try
            {
                File.AppendAllText(githubStepSummary, markdown + Environment.NewLine);
                SkipperLogger.Log($"[skipper] Summary written to GitHub Actions step summary.");
            }
            catch (Exception ex)
            {
                SkipperLogger.Warn($"[skipper] Failed to write GitHub summary: {ex.Message}");
                SkipperLogger.Log(markdown);
            }
        }
        else
        {
            SkipperLogger.Log(markdown);
        }
    }

    /// <summary>
    /// Executes the full reporting pipeline: generate, write JSON, write summary.
    /// </summary>
    public void ExecuteReport()
    {
        var report = GenerateReport();
        WriteReportJson(report);
        var markdown = GenerateMarkdownSummary(report);
        WriteSummaryToGitHub(markdown);
    }

    // Helper to expose cache entries (internal, for testing)
    internal IEnumerable<KeyValuePair<string, string?>> GetAllCacheEntries() =>
        _resolver.GetCacheEntriesForReporting();
}

/// <summary>
/// Represents a quarantine report with metrics about suppressed tests.
/// </summary>
public sealed class QuarantineReport
{
    [JsonPropertyName("generatedAtUtc")]
    public DateTimeOffset GeneratedAtUtc { get; set; }

    [JsonPropertyName("totalSuppressed")]
    public int TotalSuppressed { get; set; }

    [JsonPropertyName("expiringThisWeek")]
    public int ExpiringThisWeek { get; set; }

    [JsonPropertyName("reenabledThisRun")]
    public int ReenabledThisRun { get; set; }

    [JsonPropertyName("quarantineDaysOfDebt")]
    public int QuarantineDaysOfDebt { get; set; }

    [JsonPropertyName("suppressedTests")]
    public List<TestMetric> SuppressedTests { get; set; } = new();

    [JsonPropertyName("reenabledTests")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string> ReenabledTests { get; set; } = new();
}

/// <summary>
/// Metric for a single suppressed test.
/// </summary>
public sealed class TestMetric
{
    [JsonPropertyName("testId")]
    public string TestId { get; set; } = string.Empty;

    [JsonPropertyName("disabledUntil")]
    public DateTimeOffset DisabledUntil { get; set; }

    [JsonPropertyName("daysRemaining")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? DaysRemaining =>
        (int)(DisabledUntil.Date - DateTimeOffset.UtcNow.Date).TotalDays;
}
