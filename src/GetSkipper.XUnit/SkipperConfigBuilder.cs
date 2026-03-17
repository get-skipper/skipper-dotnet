using GetSkipper.Core;
using GetSkipper.Core.Credentials;
using System.Reflection;

namespace GetSkipper.XUnit;

/// <summary>
/// Builds a <see cref="SkipperConfig"/> from a <see cref="SkipperConfigAttribute"/>
/// found on the test assembly.
/// </summary>
internal static class SkipperConfigBuilder
{
    internal static SkipperConfig Build(Assembly assembly)
    {
        var attr = assembly.GetCustomAttribute<SkipperConfigAttribute>()
            ?? throw new InvalidOperationException(
                "[skipper] No [assembly: SkipperConfig(...)] attribute found. " +
                "Add it to your AssemblyInfo.cs or any .cs file in the test project.");

        var spreadsheetId = attr.SpreadsheetId;
        if (spreadsheetId is null && attr.SpreadsheetIdEnvVar is not null)
        {
            spreadsheetId = Environment.GetEnvironmentVariable(attr.SpreadsheetIdEnvVar)
                ?? throw new InvalidOperationException(
                    $"[skipper] Environment variable \"{attr.SpreadsheetIdEnvVar}\" is not set.");
        }
        if (string.IsNullOrWhiteSpace(spreadsheetId))
            throw new InvalidOperationException(
                "[skipper] [SkipperConfig] requires SpreadsheetId or SpreadsheetIdEnvVar.");

        ISkipperCredentials credentials;

        if (attr.CredentialsFile is not null)
            credentials = new FileCredentials(attr.CredentialsFile);
        else if (attr.CredentialsBase64 is not null)
            credentials = new Base64Credentials(attr.CredentialsBase64);
        else if (attr.CredentialsEnvVar is not null)
        {
            var value = Environment.GetEnvironmentVariable(attr.CredentialsEnvVar)
                ?? throw new InvalidOperationException(
                    $"[skipper] Environment variable \"{attr.CredentialsEnvVar}\" is not set.");
            credentials = new Base64Credentials(value);
        }
        else
            throw new InvalidOperationException(
                "[skipper] [SkipperConfig] requires one of: CredentialsFile, CredentialsBase64, or CredentialsEnvVar.");

        var referenceSheets = string.IsNullOrWhiteSpace(attr.ReferenceSheets)
            ? []
            : (IReadOnlyList<string>)attr.ReferenceSheets
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new SkipperConfig
        {
            SpreadsheetId = spreadsheetId,
            Credentials = credentials,
            SheetName = attr.SheetName,
            ReferenceSheets = referenceSheets,
        };
    }
}
