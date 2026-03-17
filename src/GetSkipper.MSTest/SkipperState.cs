using GetSkipper.Core;

namespace GetSkipper.MSTest;

/// <summary>
/// Singleton that holds the initialised <see cref="SkipperResolver"/> and
/// the list of discovered test IDs for the current MSTest run.
/// </summary>
internal static class SkipperState
{
    private static SkipperResolver? _resolver;
    private static readonly List<string> _discoveredIds = [];
    private static readonly Lock _lock = new();

    internal static SkipperResolver? Resolver
    {
        get => _resolver;
        set => _resolver = value;
    }

    internal static bool IsInitialized => _resolver is not null;

    internal static void RecordDiscoveredId(string testId)
    {
        lock (_lock) { _discoveredIds.Add(testId); }
    }

    internal static IReadOnlyList<string> GetDiscoveredIds()
    {
        lock (_lock) { return [.. _discoveredIds]; }
    }
}
