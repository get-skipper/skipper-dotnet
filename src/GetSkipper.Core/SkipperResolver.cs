using System.Text.Encodings.Web;
using System.Text.Json;
using Google.Apis.Sheets.v4;

namespace GetSkipper.Core;

/// <summary>
/// Primary integration point for all framework adapters.
///
/// <para>Lifecycle:</para>
/// <list type="number">
///   <item>Call <see cref="InitializeAsync"/> once before tests run (main process / global setup).</item>
///   <item>Call <see cref="IsTestEnabled"/> per test to decide whether to skip.</item>
///   <item>Serialise with <see cref="ToJson"/> / deserialise with <see cref="FromJson"/>
///   to share the in-memory cache across worker processes without re-authenticating.</item>
/// </list>
/// </summary>
public sealed class SkipperResolver
{
    private readonly SkipperConfig _config;
    private readonly SheetsClient _client;

    // normalizedTestId → ISO-8601 disabledUntil string, or null (enabled, no date set)
    private Dictionary<string, string?> _cache = new(StringComparer.OrdinalIgnoreCase);

    // Keep the SheetsService alive so the SheetsWriter can reuse it without re-authenticating
    private SheetsService? _service;

    private bool _initialized;

    /// <summary>Path to the local fallback cache file. Defaults to <c>.skipper-cache.json</c>
    /// in the current working directory.</summary>
    public string LocalCacheFile { get; set; } = ".skipper-cache.json";

    public SkipperResolver(SkipperConfig config)
    {
        _config = config;
        _client = new SheetsClient(config);
    }

    /// <summary>
    /// Fetches the spreadsheet and populates the in-memory cache.
    /// Must be called exactly once before <see cref="IsTestEnabled"/>.
    ///
    /// <para>Environment variables that affect behaviour:</para>
    /// <list type="bullet">
    ///   <item><c>SKIPPER_FAIL_OPEN</c> (default <c>true</c>) — when the API is unreachable and no
    ///   valid cache exists, run all tests instead of throwing.</item>
    ///   <item><c>SKIPPER_CACHE_TTL</c> (default <c>300</c> seconds) — after a successful fetch
    ///   the result is written to <see cref="LocalCacheFile"/>. On the next API failure the file
    ///   is used if it was written within this many seconds.</item>
    /// </list>
    /// </summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var failOpen = ReadBoolEnv("SKIPPER_FAIL_OPEN", defaultValue: true);
        var cacheTtl = ReadIntEnv("SKIPPER_CACHE_TTL", defaultValue: 300);

        try
        {
            var result = await _client.FetchAllAsync(ct);
            _service = result.Service;

            _cache = result.Entries.ToDictionary(
                e => TestIdHelper.Normalize(e.TestId),
                e => e.DisabledUntil?.ToString("O"),   // ISO-8601 round-trip
                StringComparer.OrdinalIgnoreCase);

            _initialized = true;
            SkipperLogger.Log($"Resolver initialised with {_cache.Count} entries.");

            // Persist a local fallback cache for future API failures.
            WriteLocalCache(cacheTtl);
        }
        catch (Exception ex) when (failOpen)
        {
            SkipperLogger.Warn($"[skipper] API call failed: {ex.Message}. Attempting local cache fallback.");

            if (TryReadLocalCache(cacheTtl))
            {
                SkipperLogger.Log($"Resolver initialised from local cache with {_cache.Count} entries.");
            }
            else
            {
                SkipperLogger.Warn("[skipper] No valid local cache found — running all tests (fail-open).");
                _cache = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            }

            _initialized = true;
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> if the test with the given <paramref name="testId"/>
    /// should run. Tests not present in the spreadsheet are considered enabled (opt-out model).
    /// </summary>
    public bool IsTestEnabled(string testId)
    {
        EnsureInitialized();
        var key = TestIdHelper.Normalize(testId);

        if (!_cache.TryGetValue(key, out var disabledUntilStr))
            return true; // unknown tests run by default

        if (string.IsNullOrEmpty(disabledUntilStr))
            return true; // in sheet but no date → enabled

        if (!DateTimeOffset.TryParse(disabledUntilStr, out var disabledUntil))
            return true; // unparseable date → enabled (fail-open)

        return DateTimeOffset.UtcNow > disabledUntil; // past the date → enabled
    }

    /// <summary>
    /// Returns the <c>disabledUntil</c> date for the given <paramref name="testId"/>,
    /// or <see langword="null"/> if not set or not present.
    /// </summary>
    public DateTimeOffset? GetDisabledUntil(string testId)
    {
        EnsureInitialized();
        var key = TestIdHelper.Normalize(testId);

        if (!_cache.TryGetValue(key, out var disabledUntilStr) ||
            string.IsNullOrEmpty(disabledUntilStr))
            return null;

        return DateTimeOffset.TryParse(disabledUntilStr, out var dt) ? dt : null;
    }

    /// <summary>
    /// Returns the current <see cref="SkipperMode"/> from the <c>SKIPPER_MODE</c> environment variable.
    /// </summary>
    public SkipperMode GetMode() => SkipperModeExtensions.FromEnvironment();

    /// <summary>
    /// Returns a <see cref="SheetsWriter"/> that reuses the already-authenticated Google Sheets
    /// service from the initialisation phase. Only valid after <see cref="InitializeAsync"/>.
    /// </summary>
    public SheetsWriter GetWriter()
    {
        EnsureInitialized();
        return new SheetsWriter(_config, _service!);
    }

    /// <summary>
    /// Serialises the cache to JSON so it can be shared with worker processes via
    /// <see cref="CacheManager.WriteResolverCache"/>.
    /// </summary>
    public string ToJson() => JsonSerializer.Serialize(_cache);

    /// <summary>
    /// Rehydrates a <see cref="SkipperResolver"/> from JSON produced by <see cref="ToJson"/>.
    /// The resulting resolver is already "initialised" and can call <see cref="IsTestEnabled"/>
    /// immediately — no network call is made.
    /// </summary>
    public static SkipperResolver FromJson(string json, SkipperConfig config)
    {
        var cache = JsonSerializer.Deserialize<Dictionary<string, string?>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? [];

        var resolver = new SkipperResolver(config)
        {
            _cache = new Dictionary<string, string?>(cache, StringComparer.OrdinalIgnoreCase),
            _initialized = true,
        };

        return resolver;
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
            throw new InvalidOperationException(
                "[skipper] SkipperResolver has not been initialised. " +
                "Call InitializeAsync() before using IsTestEnabled().");
    }

    // ── Local cache helpers ────────────────────────────────────────────────

    private void WriteLocalCache(int ttlSeconds)
    {
        try
        {
            var payload = new LocalCachePayload
            {
                WrittenAtUtcUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                TtlSeconds = ttlSeconds,
                Entries = _cache,
            };
            var json = JsonSerializer.Serialize(payload,
                new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            File.WriteAllText(LocalCacheFile, json);
            SkipperLogger.Log($"[skipper] Local cache written to {LocalCacheFile}.");
        }
        catch (Exception ex)
        {
            SkipperLogger.Warn($"[skipper] Failed to write local cache: {ex.Message}");
        }
    }

    private bool TryReadLocalCache(int ttlSeconds)
    {
        try
        {
            if (!File.Exists(LocalCacheFile))
                return false;

            var json = File.ReadAllText(LocalCacheFile);
            var payload = JsonSerializer.Deserialize<LocalCachePayload>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (payload is null || payload.Entries is null)
                return false;

            var ageSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - payload.WrittenAtUtcUnix;
            if (ageSeconds > ttlSeconds)
            {
                SkipperLogger.Warn($"[skipper] Local cache is {ageSeconds}s old (TTL={ttlSeconds}s) — ignoring.");
                return false;
            }

            _cache = new Dictionary<string, string?>(payload.Entries, StringComparer.OrdinalIgnoreCase);
            return true;
        }
        catch (Exception ex)
        {
            SkipperLogger.Warn($"[skipper] Failed to read local cache: {ex.Message}");
            return false;
        }
    }

    // ── Env-var helpers ───────────────────────────────────────────────────

    private static bool ReadBoolEnv(string name, bool defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(raw)) return defaultValue;
        return !string.Equals(raw.Trim(), "false", StringComparison.OrdinalIgnoreCase)
            && raw.Trim() != "0";
    }

    private static int ReadIntEnv(string name, int defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        return int.TryParse(raw, out var v) ? v : defaultValue;
    }

    /// <summary>
    /// Exposes the cache entries for reporting. Used by <see cref="SkipperReporter"/>.
    /// </summary>
    internal IEnumerable<KeyValuePair<string, string?>> GetCacheEntriesForReporting()
    {
        EnsureInitialized();
        return _cache;
    }

    // ── Internal test-only hooks ──────────────────────────────────────────

    /// <summary>For unit-testing only — exposes <see cref="WriteLocalCache"/>.</summary>
    internal void WriteLocalCacheForTest() =>
        WriteLocalCache(ReadIntEnv("SKIPPER_CACHE_TTL", 300));

    /// <summary>For unit-testing only — exposes <see cref="TryReadLocalCache"/>.</summary>
    internal bool TryReadLocalCacheForTest(int ttlSeconds) =>
        TryReadLocalCache(ttlSeconds);

    /// <summary>For unit-testing only — reads the SKIPPER_FAIL_OPEN env var.</summary>
    internal static bool ReadFailOpenForTest() =>
        ReadBoolEnv("SKIPPER_FAIL_OPEN", defaultValue: true);

    // ── Nested types ──────────────────────────────────────────────────────

    private sealed class LocalCachePayload
    {
        public long WrittenAtUtcUnix { get; set; }
        public int TtlSeconds { get; set; }
        public Dictionary<string, string?> Entries { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
