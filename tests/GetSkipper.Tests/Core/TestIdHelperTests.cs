using GetSkipper.Core;
using Xunit;

namespace GetSkipper.Tests.Core;

public sealed class TestIdHelperTests
{
    [Theory]
    [InlineData("  Hello World  ", "hello world")]
    [InlineData("Hello   World", "hello world")]
    [InlineData("HELLO WORLD", "hello world")]
    [InlineData("hello world", "hello world")]
    public void Normalize_TrimsAndLowercasesAndCollapsesWhitespace(string input, string expected)
    {
        Assert.Equal(expected, TestIdHelper.Normalize(input));
    }

    [Fact]
    public void Build_CombinesFilePathAndTitleParts()
    {
        var result = TestIdHelper.Build("Tests/Unit/AuthTests.cs", ["AuthTests", "CanLogin"]);
        Assert.Equal("Tests/Unit/AuthTests.cs > AuthTests > CanLogin", result);
    }

    [Fact]
    public void Build_NormalizesBackslashesToForwardSlashes()
    {
        var result = TestIdHelper.Build(@"Tests\Unit\AuthTests.cs", ["AuthTests", "CanLogin"]);
        Assert.Equal("Tests/Unit/AuthTests.cs > AuthTests > CanLogin", result);
    }

    [Fact]
    public void Build_MakesAbsolutePathRelativeToCwd()
    {
        var cwd = Directory.GetCurrentDirectory();
        var absPath = Path.Combine(cwd, "Tests", "AuthTests.cs");
        var result = TestIdHelper.Build(absPath, ["AuthTests", "CanLogin"]);
        Assert.Equal("Tests/AuthTests.cs > AuthTests > CanLogin", result);
    }
}
