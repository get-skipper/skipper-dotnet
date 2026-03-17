using System.Text.RegularExpressions;

namespace GetSkipper.Core;

/// <summary>
/// Utilities for building and normalising test IDs.
/// </summary>
public static partial class TestIdHelper
{
    /// <summary>
    /// Normalises a test ID for consistent comparison:
    /// trims whitespace, converts to lowercase, and collapses internal whitespace runs to a single space.
    /// </summary>
    public static string Normalize(string id) =>
        WhitespaceRegex().Replace(id.Trim(), " ").ToLowerInvariant();

    /// <summary>
    /// Builds a canonical test ID in the format:
    /// <c>"relative/path/to/file.cs &gt; ClassName &gt; MethodName"</c>.
    /// </summary>
    /// <param name="filePath">
    /// Absolute or relative path to the test file.
    /// If absolute, it is made relative to <see cref="Directory.GetCurrentDirectory"/>.
    /// Path separators are normalised to <c>/</c>.
    /// </param>
    /// <param name="titlePath">Ordered parts of the test title (class name, method name, describe blocks, etc.).</param>
    public static string Build(string filePath, IEnumerable<string> titlePath)
    {
        if (Path.IsPathRooted(filePath))
        {
            var cwd = Directory.GetCurrentDirectory();
            if (filePath.StartsWith(cwd, StringComparison.OrdinalIgnoreCase))
                filePath = filePath[cwd.Length..].TrimStart(Path.DirectorySeparatorChar);
        }

        filePath = filePath.Replace(Path.DirectorySeparatorChar, '/');
        return string.Join(" > ", new[] { filePath }.Concat(titlePath));
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
