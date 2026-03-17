using Xunit;
using GetSkipper.XUnit;

[assembly: TestFramework("GetSkipper.XUnit.SkipperTestFramework", "GetSkipper.XUnit")]
[assembly: SkipperConfig(
    SpreadsheetIdEnvVar = "SKIPPER_SPREADSHEET_ID",
    CredentialsEnvVar = "GOOGLE_CREDS_B64",
    SheetName = "skipper-dotnet")]
