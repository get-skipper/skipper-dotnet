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

    public SkipperResolver(SkipperConfig config)
    {
        _config = config;
        _client = new SheetsClient(config);
    }

    /// <summary>
    /// Fetches the spreadsheet and populates the in-memory cache.
    /// Must be called exactly once before <see cref="IsTestEnabled"/>.
    /// </summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var result = await _client.FetchAllAsync(ct);
        _service = result.Service;

        _cache = result.Entries.ToDictionary(
            e => TestIdHelper.Normalize(e.TestId),
            e => e.DisabledUntil?.ToString("O"),   // ISO-8601 round-trip
            StringComparer.OrdinalIgnoreCase);

        _initialized = true;
        SkipperLogger.Log($"Resolver initialised with {_cache.Count} entries.");
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
}
