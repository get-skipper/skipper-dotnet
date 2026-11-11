using GetSkipper.XUnit;
using Xunit;

namespace GetSkipper.Tests.XUnit;

public sealed class SkipperTestCaseRunnerTests
{
    [Theory]
    [InlineData("Tests.Unit.AuthTests", "CanLogin", "Tests/Unit/AuthTests.cs > AuthTests > CanLogin")]
    [InlineData("AuthTests", "CanLogin", "AuthTests.cs > AuthTests > CanLogin")]
    public void BuildTestId_UsesNamespacePath(string fqClassName, string method, string expected)
    {
        Assert.Equal(expected, SkipperTestCaseRunner.BuildTestId(fqClassName, method));
    }
}
