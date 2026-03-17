namespace GetSkipper.Core;

/// <summary>
/// Represents a single row in the Google Spreadsheet.
/// </summary>
/// <param name="TestId">The canonical test identifier string.</param>
/// <param name="DisabledUntil">
/// When set and in the future, the test should be skipped.
/// <see langword="null"/> means the test is enabled.
/// </param>
/// <param name="Notes">Optional free-text notes column.</param>
public sealed record TestEntry(
    string TestId,
    DateTimeOffset? DisabledUntil,
    string? Notes = null);
