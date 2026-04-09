using System.Text.Json;
using GetSkipper.Core;
using GetSkipper.Core.Credentials;
using Xunit;

namespace GetSkipper.Tests.Core;

/// <summary>
/// Tests for SKIPPER_FAIL_OPEN, SKIPPER_CACHE_TTL, and SKIPPER_SYNC_ALLOW_DELETE behaviour.
/// These tests exercise the env-var driven safety features via <see cref="SkipperResolver.FromJson"/>
/// and direct inspection of the local cache file.
/// </summary>
public sealed class SafetyTrinityTests : IDisposable
{
    private readonly string _cacheFile;

    public SafetyTrinityTests()
    {
        _cacheFile = Path.Combine(Path.GetTempPath(), $"skipper-test-cache-{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_cacheFile)) File.Delete(_cacheFile);
        Environment.SetEnvironmentVariable("SKIPPER_FAIL_OPEN", null);
        Environment.SetEnvironmentVariable("SKIPPER_CACHE_TTL", null);
        Environment.SetEnvironmentVariable("SKIPPER_SYNC_ALLOW_DELETE", null);
    }

    private static SkipperConfig MakeConfig() => new()
    {
        SpreadsheetId = "fake-id",
        Credentials = new ServiceAccountCredentials("test@test.iam.gserviceaccount.com", "fake-key"),
    };

    // ── SKIPPER_CACHE_TTL: local cache written / read ─────────────────────

    [Fact]
    public void LocalCacheFile_WrittenAfterSuccessfulInit()
    {
        // Arrange: use FromJson to simulate a successful fetch result.
        var future = DateTimeOffset.UtcNow.AddDays(1).ToString("O");
        var resolver = SkipperResolver.FromJson(
            $$"""{"tests/foo.cs > foo > bar": "{{future}}"}""", MakeConfig());
        resolver.LocalCacheFile = _cacheFile;

        // Act: write cache manually (mirrors what InitializeAsync does internally).
        resolver.WriteLocalCacheForTest();

        // Assert
        Assert.True(File.Exists(_cacheFile), "Local cache file should have been created.");
        var json = File.ReadAllText(_cacheFile);
        Assert.Contains("tests/foo.cs > foo > bar", json);
    }

    [Fact]
    public void LocalCache_RestoredWhenWithinTtl()
    {
        // Pre-write a fresh cache file.
        var future = DateTimeOffset.UtcNow.AddDays(1).ToString("O");
        var payload = new
        {
            writtenAtUtcUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ttlSeconds = 300,
            entries = new Dictionary<string, string?> { ["tests/foo.cs > foo > bar"] = future },
        };
        File.WriteAllText(_cacheFile, JsonSerializer.Serialize(payload));

        Environment.SetEnvironmentVariable("SKIPPER_CACHE_TTL", "300");

        var resolver = new SkipperResolver(MakeConfig()) { LocalCacheFile = _cacheFile };
        var restored = resolver.TryReadLocalCacheForTest(300);

        Assert.True(restored, "Cache within TTL should be loaded.");
    }

    [Fact]
    public void LocalCache_RejectedWhenExpired()
    {
        // Write a cache that is older than the TTL.
        var past = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 600; // 10 minutes ago
        var payload = new
        {
            writtenAtUtcUnix = past,
            ttlSeconds = 300,
            entries = new Dictionary<string, string?>(),
        };
        File.WriteAllText(_cacheFile, JsonSerializer.Serialize(payload));

        var resolver = new SkipperResolver(MakeConfig()) { LocalCacheFile = _cacheFile };
        var restored = resolver.TryReadLocalCacheForTest(300);

        Assert.False(restored, "Expired cache should not be loaded.");
    }

    // ── SKIPPER_FAIL_OPEN (behaviour is integration-level; env var reading tested here) ──

    [Theory]
    [InlineData(null, true)]    // default
    [InlineData("true", true)]
    [InlineData("1", true)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    [InlineData("FALSE", false)]
    public void ReadBoolEnv_SkipperFailOpen_ParsedCorrectly(string? value, bool expected)
    {
        Environment.SetEnvironmentVariable("SKIPPER_FAIL_OPEN", value);
        Assert.Equal(expected, SkipperResolver.ReadFailOpenForTest());
    }

    // ── SKIPPER_SYNC_ALLOW_DELETE ──────────────────────────────────────────

    [Theory]
    [InlineData(null, false)]   // default: do NOT delete
    [InlineData("false", false)]
    [InlineData("0", false)]
    [InlineData("true", true)]
    [InlineData("1", true)]
    [InlineData("TRUE", true)]
    public void SkipperSyncAllowDelete_ParsedCorrectly(string? value, bool expected)
    {
        Environment.SetEnvironmentVariable("SKIPPER_SYNC_ALLOW_DELETE", value);
        Assert.Equal(expected, SheetsWriter.ReadAllowDeleteForTest());
    }
}
