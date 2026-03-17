using GetSkipper.Core.Credentials;

namespace GetSkipper.Core;

/// <summary>
/// Configuration for Skipper. Construct once and pass to the framework-specific
/// integration class (e.g., the assembly attribute or the global hook class).
/// </summary>
public sealed class SkipperConfig
{
    /// <summary>Google Spreadsheet ID (from the URL).</summary>
    public required string SpreadsheetId { get; init; }

    /// <summary>
    /// Service account credentials.
    /// Choose one of: <see cref="FileCredentials"/>, <see cref="Base64Credentials"/>,
    /// or <see cref="ServiceAccountCredentials"/>.
    /// </summary>
    public required ISkipperCredentials Credentials { get; init; }

    /// <summary>
    /// Name of the primary sheet tab. Defaults to the first sheet in the spreadsheet.
    /// </summary>
    public string? SheetName { get; init; }

    /// <summary>
    /// Additional read-only sheet tab names. Entries are merged with the primary sheet.
    /// When the same test ID appears in multiple sheets the most restrictive
    /// (latest) <c>disabledUntil</c> date wins.
    /// </summary>
    public IReadOnlyList<string> ReferenceSheets { get; init; } = [];

    /// <summary>Column header for test IDs. Defaults to <c>"testId"</c>.</summary>
    public string TestIdColumn { get; init; } = "testId";

    /// <summary>Column header for the disabled-until date. Defaults to <c>"disabledUntil"</c>.</summary>
    public string DisabledUntilColumn { get; init; } = "disabledUntil";
}
