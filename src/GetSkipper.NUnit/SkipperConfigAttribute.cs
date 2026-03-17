namespace GetSkipper.NUnit;

/// <summary>
/// Assembly-level attribute that configures Skipper for NUnit.
/// Apply once in your test project (e.g., <c>AssemblyInfo.cs</c>):
/// <code>
/// [assembly: SkipperConfig(SpreadsheetId = "1abc...", CredentialsFile = "service-account.json")]
/// </code>
/// No other changes to your tests are required.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class SkipperConfigAttribute : Attribute
{
    /// <summary>Google Spreadsheet ID (from the URL).</summary>
    public required string SpreadsheetId { get; init; }

    /// <summary>Path to the service account JSON file.</summary>
    public string? CredentialsFile { get; init; }

    /// <summary>Base-64 encoded service account JSON (for CI).</summary>
    public string? CredentialsBase64 { get; init; }

    /// <summary>Name of an environment variable containing base-64 encoded credentials.</summary>
    public string? CredentialsEnvVar { get; init; }

    /// <summary>Sheet tab name. Defaults to the first sheet.</summary>
    public string? SheetName { get; init; }

    /// <summary>Comma-separated list of read-only reference sheet tab names.</summary>
    public string? ReferenceSheets { get; init; }
}
