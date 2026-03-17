using GetSkipper.Core;
using GetSkipper.Core.Credentials;
using Xunit;

namespace GetSkipper.Tests.Core;

public sealed class SkipperResolverTests
{
    private static SkipperConfig MakeConfig() => new()
    {
        SpreadsheetId = "fake-id",
        Credentials = new ServiceAccountCredentials("test@test.iam.gserviceaccount.com", "fake-key"),
    };

    [Fact]
    public void IsTestEnabled_ReturnsTrueForUnknownTest()
    {
        var resolver = SkipperResolver.FromJson("{}", MakeConfig());
        Assert.True(resolver.IsTestEnabled("any/test > Foo > Bar"));
    }

    [Fact]
    public void IsTestEnabled_ReturnsTrueWhenNullDisabledUntil()
    {
        var json = """{"tests/foo.cs > foo > bar": null}""";
        var resolver = SkipperResolver.FromJson(json, MakeConfig());
        Assert.True(resolver.IsTestEnabled("tests/foo.cs > Foo > Bar"));
    }

    [Fact]
    public void IsTestEnabled_ReturnsFalseWhenDisabledUntilInFuture()
    {
        var future = DateTimeOffset.UtcNow.AddYears(1).ToString("O");
        var json = $$"""{"tests/foo.cs > foo > bar": "{{future}}"}""";
        var resolver = SkipperResolver.FromJson(json, MakeConfig());
        Assert.False(resolver.IsTestEnabled("tests/foo.cs > Foo > Bar"));
    }

    [Fact]
    public void IsTestEnabled_ReturnsTrueWhenDisabledUntilInPast()
    {
        var past = DateTimeOffset.UtcNow.AddYears(-1).ToString("O");
        var json = $$"""{"tests/foo.cs > foo > bar": "{{past}}"}""";
        var resolver = SkipperResolver.FromJson(json, MakeConfig());
        Assert.True(resolver.IsTestEnabled("tests/foo.cs > Foo > Bar"));
    }

    [Fact]
    public void IsTestEnabled_IsCaseInsensitive()
    {
        var future = DateTimeOffset.UtcNow.AddYears(1).ToString("O");
        var json = $$"""{"TESTS/FOO.CS > FOO > BAR": "{{future}}"}""";
        var resolver = SkipperResolver.FromJson(json, MakeConfig());
        Assert.False(resolver.IsTestEnabled("tests/foo.cs > Foo > Bar"));
    }

    [Fact]
    public void FromJson_ToJson_RoundTrips()
    {
        var future = DateTimeOffset.UtcNow.AddYears(1).ToString("O");
        var original = SkipperResolver.FromJson(
            $$"""{"tests/foo.cs > foo > bar": "{{future}}"}""",
            MakeConfig());

        var json = original.ToJson();
        var restored = SkipperResolver.FromJson(json, MakeConfig());

        Assert.False(restored.IsTestEnabled("tests/foo.cs > foo > bar"));
    }

    [Fact]
    public void IsTestEnabled_ThrowsBeforeInitialization()
    {
        var resolver = new SkipperResolver(MakeConfig());
        var ex = Assert.Throws<InvalidOperationException>(
            () => resolver.IsTestEnabled("any"));
        Assert.Contains("InitializeAsync", ex.Message);
    }

    [Fact]
    public void GetDisabledUntil_ReturnsNullForUnknownTest()
    {
        var resolver = SkipperResolver.FromJson("{}", MakeConfig());
        Assert.Null(resolver.GetDisabledUntil("unknown"));
    }

    [Fact]
    public void GetDisabledUntil_ReturnsDateForDisabledTest()
    {
        var future = DateTimeOffset.UtcNow.AddDays(10);
        var json = $$"""{"tests/foo.cs > foo > bar": "{{future:O}}"}""";
        var resolver = SkipperResolver.FromJson(json, MakeConfig());
        var result = resolver.GetDisabledUntil("tests/foo.cs > foo > bar");
        Assert.NotNull(result);
        Assert.True(result > DateTimeOffset.UtcNow);
    }
}
