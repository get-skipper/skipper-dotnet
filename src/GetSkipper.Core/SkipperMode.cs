namespace GetSkipper.Core;

/// <summary>
/// Controls whether Skipper only reads the spreadsheet (default) or also
/// reconciles it with the tests discovered during this run.
/// </summary>
public enum SkipperMode
{
    /// <summary>Only read the spreadsheet — never write to it. This is the default.</summary>
    ReadOnly,

    /// <summary>
    /// After tests complete, append new test IDs that were discovered but not yet in the
    /// spreadsheet, and remove rows for tests that no longer exist.
    /// Activate with the <c>SKIPPER_MODE=sync</c> environment variable.
    /// </summary>
    Sync,
}

/// <summary>Helper to read <see cref="SkipperMode"/> from the environment.</summary>
public static class SkipperModeExtensions
{
    /// <summary>
    /// Returns <see cref="SkipperMode.Sync"/> if <c>SKIPPER_MODE=sync</c> (case-insensitive),
    /// otherwise <see cref="SkipperMode.ReadOnly"/>.
    /// </summary>
    public static SkipperMode FromEnvironment()
    {
        var value = Environment.GetEnvironmentVariable("SKIPPER_MODE");
        return string.Equals(value, "sync", StringComparison.OrdinalIgnoreCase)
            ? SkipperMode.Sync
            : SkipperMode.ReadOnly;
    }
}
