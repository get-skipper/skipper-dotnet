using GetSkipper.Core;
using Xunit;

namespace GetSkipper.Tests.Core;

public sealed class CacheManagerTests : IDisposable
{
    private string? _tempDir;

    [Fact]
    public void WriteResolverCache_CreatesFileAndSetsEnvVars()
    {
        var json = """{"key": "value"}""";
        _tempDir = CacheManager.WriteResolverCache(json);

        Assert.True(Directory.Exists(_tempDir));
        var cacheFile = CacheManager.GetCacheFilePath();
        Assert.NotNull(cacheFile);
        Assert.True(File.Exists(cacheFile));
        Assert.Equal(json, File.ReadAllText(cacheFile));
        Assert.NotNull(CacheManager.GetDiscoveredDir());
    }

    [Fact]
    public void WriteAndReadResolverCache_RoundTrips()
    {
        var json = """{"a":"b"}""";
        _tempDir = CacheManager.WriteResolverCache(json);
        var path = CacheManager.GetCacheFilePath()!;
        Assert.Equal(json, CacheManager.ReadResolverCache(path));
    }

    [Fact]
    public void WriteDiscoveredIds_ThenMerge_ReturnsDeduplicated()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"skipper-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        _tempDir = dir;

        CacheManager.WriteDiscoveredIds(dir, ["id1", "id2"]);
        CacheManager.WriteDiscoveredIds(dir, ["id2", "id3"]);

        var merged = CacheManager.MergeDiscoveredIds(dir).ToList();
        Assert.Equal(3, merged.Count);
        Assert.Contains("id1", merged);
        Assert.Contains("id2", merged);
        Assert.Contains("id3", merged);
    }

    [Fact]
    public void Cleanup_RemovesTempDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"skipper-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        CacheManager.Cleanup(dir);
        Assert.False(Directory.Exists(dir));
    }

    public void Dispose()
    {
        if (_tempDir is not null && Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
