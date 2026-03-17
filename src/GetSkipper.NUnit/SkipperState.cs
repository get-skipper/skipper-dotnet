using GetSkipper.Core;

namespace GetSkipper.NUnit;

/// <summary>
/// Singleton that holds the initialised <see cref="SkipperResolver"/> and
/// collected discovered test IDs for the current NUnit run.
/// </summary>
internal static class SkipperState
{
    private static SkipperResolver? _resolver;
    private static readonly List<string> _discoveredIds = [];
    private static readonly object _lock = new();

    internal static bool IsResolverSet => _resolver is not null;

    internal static SkipperResolver Resolver
    {
        get => _resolver ?? throw new InvalidOperationException(
            "[skipper] SkipperResolver is not initialised. " +
            "Ensure [assembly: SkipperConfig(...)] is present in your test project.");
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
