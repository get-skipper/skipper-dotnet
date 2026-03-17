namespace GetSkipper.Core;

/// <summary>
/// Simple logger that only emits output when the <c>SKIPPER_DEBUG</c> environment
/// variable is set to any non-empty value.
/// </summary>
public static class SkipperLogger
{
    private static bool IsEnabled =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SKIPPER_DEBUG"));

    /// <summary>Writes an informational message to stdout.</summary>
    public static void Log(string message)
    {
        if (IsEnabled) Console.WriteLine($"[skipper] {message}");
    }

    /// <summary>Writes a warning message to stderr.</summary>
    public static void Warn(string message)
    {
        if (IsEnabled) Console.Error.WriteLine($"[skipper] WARN: {message}");
    }

    /// <summary>Writes an error message to stderr.</summary>
    public static void Error(string message)
    {
        if (IsEnabled) Console.Error.WriteLine($"[skipper] ERROR: {message}");
    }
}
