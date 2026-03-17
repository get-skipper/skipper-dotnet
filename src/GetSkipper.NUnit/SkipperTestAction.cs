using GetSkipper.Core;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace GetSkipper.NUnit;

/// <summary>
/// NUnit action attribute that intercepts every test before it runs and
/// skips it if Skipper marks it as disabled.
///
/// Applied at <strong>assembly level</strong> by <see cref="SkipperSetUpFixture"/>
/// — no per-test or per-class annotation required.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SkipperTestAction : Attribute, ITestAction
{
    public ActionTargets Targets => ActionTargets.Test;

    public void BeforeTest(ITest test)
    {
        // Only intercept leaf tests (not suites)
        if (test.IsSuite) return;

        var resolver = SkipperState.Resolver;

        // Build test ID from the test's source file and title path
        var sourceFile = test.Method?.MethodInfo
            .DeclaringType?.Assembly
            .Location ?? string.Empty;

        // NUnit provides the full name as "Namespace.ClassName.MethodName"
        // We reconstruct it as "path > ClassName > MethodName"
        var className = test.Method?.MethodInfo.DeclaringType?.Name ?? string.Empty;
        var methodName = test.Method?.Name ?? string.Empty;

        // Try to get a relative source path from the test fixture's source file
        var filePath = GetSourceFilePath(test) ?? sourceFile;
        var testId = TestIdHelper.Build(filePath, [className, methodName]);

        SkipperState.RecordDiscoveredId(testId);

        if (!resolver.IsTestEnabled(testId))
        {
            var disabledUntil = resolver.GetDisabledUntil(testId);
            var reason = disabledUntil.HasValue
                ? $"Disabled by Skipper until {disabledUntil:yyyy-MM-dd}"
                : "Disabled by Skipper";

            SkipperLogger.Log($"Skipping \"{testId}\" — {reason}");
            Assert.Ignore(reason);
        }
    }

    public void AfterTest(ITest test) { }

    private static string? GetSourceFilePath(ITest test)
    {
        // NUnit doesn't expose source file paths at runtime through ITest.
        // We use the assembly location as a fallback. Integrations that have
        // source information (e.g., via stack traces) can override this.
        var type = test.Method?.MethodInfo.DeclaringType;
        if (type is null) return null;

        // Derive a pseudo-path from the type's namespace + name for consistent IDs
        var ns = type.Namespace?.Replace('.', '/') ?? string.Empty;
        return string.IsNullOrEmpty(ns)
            ? $"{type.Name}.cs"
            : $"{ns}/{type.Name}.cs";
    }
}
