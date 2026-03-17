// Integration tests: activate Skipper via the transparent xUnit TestFramework hook.
// Set environment variables before running:
//   SKIPPER_SPREADSHEET_ID  — Google Spreadsheet ID
//   GOOGLE_CREDS_B64        — Base-64 encoded service account JSON
//
// Run: SKIPPER_SPREADSHEET_ID=1abc... GOOGLE_CREDS_B64=... dotnet test tests/GetSkipper.Integration.Tests
// Run in sync mode: SKIPPER_MODE=sync SKIPPER_SPREADSHEET_ID=... GOOGLE_CREDS_B64=... dotnet test ...

using GetSkipper.XUnit;

[assembly: TestFramework("GetSkipper.XUnit.SkipperTestFramework", "GetSkipper.XUnit")]
[assembly: SkipperConfig(
    SpreadsheetIdEnvVar = "SKIPPER_SPREADSHEET_ID",
    CredentialsEnvVar = "GOOGLE_CREDS_B64",
    SheetName = "skipper-dotnet")]
