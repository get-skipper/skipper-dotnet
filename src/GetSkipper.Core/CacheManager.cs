using System.Text.Json;

namespace GetSkipper.Core;

/// <summary>
/// Manages the cross-process cache used when tests run in parallel workers.
///
/// <para>Flow:</para>
/// <list type="number">
///   <item>Main process calls <see cref="WriteResolverCache"/> after initialising the resolver.
///   The cache file path is stored in <c>SKIPPER_CACHE_FILE</c> and the discovered-IDs directory
///   path in <c>SKIPPER_DISCOVERED_DIR</c>.</item>
///   <item>Each worker reads the cache with <see cref="ReadResolverCache"/> and calls
///   <see cref="WriteDiscoveredIds"/> after its tests run.</item>
///   <item>The main process calls <see cref="MergeDiscoveredIds"/> to collect all discovered IDs,
///   then <see cref="Cleanup"/> to remove the temp directory.</item>
/// </list>
/// </summary>
public static class CacheManager
{
    private const string CacheFileEnvVar = "SKIPPER_CACHE_FILE";
    private const string DiscoveredDirEnvVar = "SKIPPER_DISCOVERED_DIR";

    /// <summary>
    /// Writes the resolver JSON cache to a temp directory and sets the
    /// <c>SKIPPER_CACHE_FILE</c> and <c>SKIPPER_DISCOVERED_DIR</c> environment variables.
    /// </summary>
    /// <returns>Path to the root temp directory.</returns>
    public static string WriteResolverCache(string resolverJson)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"skipper-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var cacheFile = Path.Combine(tempDir, "cache.json");
        File.WriteAllText(cacheFile, resolverJson);

        var discoveredDir = Path.Combine(tempDir, "discovered");
        Directory.CreateDirectory(discoveredDir);

        Environment.SetEnvironmentVariable(CacheFileEnvVar, cacheFile);
        Environment.SetEnvironmentVariable(DiscoveredDirEnvVar, discoveredDir);

        SkipperLogger.Log($"Cache written to {cacheFile}");
        return tempDir;
    }

    /// <summary>Reads and returns the resolver JSON from the given cache file path.</summary>
    public static string ReadResolverCache(string cacheFile)
    {
        if (!File.Exists(cacheFile))
            throw new FileNotFoundException($"[skipper] Cache file not found: {cacheFile}");

        return File.ReadAllText(cacheFile);
    }

    /// <summary>
    /// Returns the cache file path from the <c>SKIPPER_CACHE_FILE</c> environment variable,
    /// or <see langword="null"/> if it is not set.
    /// </summary>
    public static string? GetCacheFilePath() =>
        Environment.GetEnvironmentVariable(CacheFileEnvVar);

    /// <summary>
    /// Returns the discovered-IDs directory from the <c>SKIPPER_DISCOVERED_DIR</c>
    /// environment variable, or <see langword="null"/> if it is not set.
    /// </summary>
    public static string? GetDiscoveredDir() =>
        Environment.GetEnvironmentVariable(DiscoveredDirEnvVar);

    /// <summary>
    /// Writes the test IDs discovered by this worker process to a unique file in
    /// <paramref name="discoveredDir"/>. Safe to call from multiple processes concurrently.
    /// </summary>
    public static void WriteDiscoveredIds(string discoveredDir, IEnumerable<string> ids)
    {
        var fileName = $"{Environment.ProcessId}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Guid.NewGuid():N}.json";
        var path = Path.Combine(discoveredDir, fileName);
        File.WriteAllText(path, JsonSerializer.Serialize(ids.ToList()));
        SkipperLogger.Log($"Discovered IDs written to {path}");
    }

    /// <summary>
    /// Reads and merges all per-worker discovered-ID files from <paramref name="discoveredDir"/>.
    /// Returns a deduplicated collection of normalised test IDs.
    /// </summary>
    public static IEnumerable<string> MergeDiscoveredIds(string discoveredDir)
    {
        if (!Directory.Exists(discoveredDir))
            return [];

        var merged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(discoveredDir, "*.json"))
        {
            var json = File.ReadAllText(file);
            var ids = JsonSerializer.Deserialize<List<string>>(json) ?? [];
            foreach (var id in ids)
                merged.Add(id);
        }

        SkipperLogger.Log($"Merged {merged.Count} discovered test ID(s).");
        return merged;
    }

    /// <summary>Removes the temp directory created by <see cref="WriteResolverCache"/>.</summary>
    public static void Cleanup(string tempDir)
    {
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, recursive: true);
            SkipperLogger.Log($"Cleaned up temp directory {tempDir}.");
        }
    }
}
