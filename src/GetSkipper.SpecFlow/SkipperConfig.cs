using GetSkipper.Core;
using GetSkipper.Core.Credentials;

namespace GetSkipper.SpecFlow;

/// <summary>
/// Factory that builds a <see cref="Core.SkipperConfig"/> from environment variables.
/// Designed for the Reqnroll dependency injection container where constructor
/// parameters come from the environment.
///
/// <para>Supported environment variables:</para>
/// <list type="bullet">
///   <item><c>SKIPPER_SPREADSHEET_ID</c> — required</item>
///   <item><c>SKIPPER_CREDENTIALS_FILE</c> — path to service account JSON</item>
///   <item><c>SKIPPER_CREDENTIALS_BASE64</c> — base-64 encoded JSON</item>
///   <item><c>SKIPPER_SHEET_NAME</c> — optional primary sheet name</item>
///   <item><c>SKIPPER_REFERENCE_SHEETS</c> — comma-separated reference sheet names</item>
/// </list>
/// </summary>
public static class SkipperConfigFactory
{
    public static Core.SkipperConfig FromEnvironment()
    {
        var spreadsheetId = Environment.GetEnvironmentVariable("SKIPPER_SPREADSHEET_ID")
            ?? throw new InvalidOperationException(
                "[skipper] SKIPPER_SPREADSHEET_ID environment variable is not set.");

        ISkipperCredentials credentials;
        var credFile = Environment.GetEnvironmentVariable("SKIPPER_CREDENTIALS_FILE");
        var credBase64 = Environment.GetEnvironmentVariable("SKIPPER_CREDENTIALS_BASE64");

        if (credFile is not null)
            credentials = new FileCredentials(credFile);
        else if (credBase64 is not null)
            credentials = new Base64Credentials(credBase64);
        else
            throw new InvalidOperationException(
                "[skipper] Set SKIPPER_CREDENTIALS_FILE or SKIPPER_CREDENTIALS_BASE64.");

        var sheetName = Environment.GetEnvironmentVariable("SKIPPER_SHEET_NAME");
        var refSheetsRaw = Environment.GetEnvironmentVariable("SKIPPER_REFERENCE_SHEETS");
        var referenceSheets = string.IsNullOrWhiteSpace(refSheetsRaw)
            ? []
            : (IReadOnlyList<string>)refSheetsRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new Core.SkipperConfig
        {
            SpreadsheetId = spreadsheetId,
            Credentials = credentials,
            SheetName = sheetName,
            ReferenceSheets = referenceSheets,
        };
    }
}
