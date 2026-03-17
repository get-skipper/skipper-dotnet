using GetSkipper.Core;

namespace GetSkipper.XUnit;

/// <summary>
/// Singleton that holds the <see cref="SkipperResolver"/> and the list of
/// discovered test IDs for the current test assembly run.
/// Shared across all test classes within a single process.
/// </summary>
internal static class SkipperState
{
    private static SkipperResolver? _resolver;
    private static readonly List<string> _discoveredIds = [];
    private static readonly Lock _lock = new();

    internal static SkipperResolver Resolver
    {
        get => _resolver ?? throw new InvalidOperationException(
            "[skipper] SkipperResolver is not initialized. " +
            "Ensure the [assembly: TestFramework(...)] and [assembly: SkipperConfig(...)] " +
            "attributes are present in your test project.");
        set => _resolver = value;
    }

    internal static void RecordDiscoveredId(string testId)
    {
        lock (_lock) { _discoveredIds.Add(testId); }
    }

    internal static IReadOnlyList<string> GetDiscoveredIds()
    {
        lock (_lock) { return [.. _discoveredIds]; }
    }
}
