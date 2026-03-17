using GetSkipper.Core;
using GetSkipper.Core.Credentials;
using NUnit.Framework;
using System.Reflection;

namespace GetSkipper.NUnit;

/// <summary>
/// NUnit <c>[SetUpFixture]</c> that initialises the Skipper resolver once before all
/// tests in the assembly and optionally syncs the spreadsheet after all tests complete.
///
/// This class is automatically discovered by NUnit because of the <c>[SetUpFixture]</c>
/// attribute. It reads the configuration from the <see cref="SkipperConfigAttribute"/>
/// applied to the test assembly.
///
/// <para>
/// You do <strong>not</strong> need to reference this class directly.
/// Simply add to your test project:
/// </para>
/// <code>
/// [assembly: SkipperConfig(SpreadsheetId = "1abc...", CredentialsFile = "service-account.json")]
/// </code>
/// </summary>
[SetUpFixture]
public sealed class SkipperSetUpFixture
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        // Walk up the call stack to find the test assembly (the one that referenced us)
        // GetCallingAssembly returns GetSkipper.NUnit; GetEntryAssembly is the test runner.
        // The test assembly is the one that has [SkipperConfig] applied.
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetCustomAttribute<SkipperConfigAttribute>() != null);

        var attr = assembly?.GetCustomAttribute<SkipperConfigAttribute>();

        if (attr is null)
        {
            SkipperLogger.Warn(
                "No [assembly: SkipperConfig(...)] found — Skipper will not run.");
            return;
        }

        var config = BuildConfig(attr);
        var resolver = new SkipperResolver(config);

        SkipperLogger.Log("Initialising Skipper resolver...");
        await resolver.InitializeAsync();
        SkipperState.Resolver = resolver;
        SkipperLogger.Log("Skipper resolver ready.");
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDownAsync()
    {
        if (SkipperState.Resolver.GetMode() != SkipperMode.Sync) return;

        SkipperLogger.Log("Syncing spreadsheet...");
        var writer = SkipperState.Resolver.GetWriter();
        await writer.SyncAsync(SkipperState.GetDiscoveredIds());
        SkipperLogger.Log("Sync complete.");
    }

    private static SkipperConfig BuildConfig(SkipperConfigAttribute attr)
    {
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
                "[skipper] [SkipperConfig] requires CredentialsFile, CredentialsBase64, or CredentialsEnvVar.");

        var referenceSheets = string.IsNullOrWhiteSpace(attr.ReferenceSheets)
            ? []
            : (IReadOnlyList<string>)attr.ReferenceSheets
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // SKIPPER_SHEET_NAME env var overrides the attribute value
        var sheetName = Environment.GetEnvironmentVariable("SKIPPER_SHEET_NAME")
            ?? attr.SheetName;

        return new SkipperConfig
        {
            SpreadsheetId = attr.SpreadsheetId,
            Credentials = credentials,
            SheetName = sheetName,
            ReferenceSheets = referenceSheets,
        };
    }
}
