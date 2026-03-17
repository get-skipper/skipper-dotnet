namespace GetSkipper.XUnit;

/// <summary>
/// Assembly-level attribute that carries the Skipper configuration.
/// Apply once alongside <c>[assembly: TestFramework(...)]</c>:
/// <code>
/// [assembly: TestFramework("GetSkipper.XUnit.SkipperTestFramework", "GetSkipper.XUnit")]
/// [assembly: SkipperConfig(SpreadsheetId = "1abc...", CredentialsFile = "service-account.json")]
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class SkipperConfigAttribute : Attribute
{
    /// <summary>Google Spreadsheet ID (from the URL). Mutually exclusive with <see cref="SpreadsheetIdEnvVar"/>.</summary>
    public string? SpreadsheetId { get; init; }

    /// <summary>Name of an environment variable containing the Google Spreadsheet ID. Mutually exclusive with <see cref="SpreadsheetId"/>.</summary>
    public string? SpreadsheetIdEnvVar { get; init; }

    /// <summary>Path to the service account JSON file. Mutually exclusive with <see cref="CredentialsBase64"/> and <see cref="CredentialsEnvVar"/>.</summary>
    public string? CredentialsFile { get; init; }

    /// <summary>Base-64 encoded service account JSON. Mutually exclusive with <see cref="CredentialsFile"/> and <see cref="CredentialsEnvVar"/>.</summary>
    public string? CredentialsBase64 { get; init; }

    /// <summary>Environment variable containing the base-64 encoded service account JSON. Mutually exclusive with <see cref="CredentialsFile"/> and <see cref="CredentialsBase64"/>.</summary>
    public string? CredentialsEnvVar { get; init; }

    /// <summary>Sheet tab name. Defaults to the first sheet.</summary>
    public string? SheetName { get; init; }

    /// <summary>Comma-separated list of read-only reference sheet tab names.</summary>
    public string? ReferenceSheets { get; init; }
}
