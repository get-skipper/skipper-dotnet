# skipper-dotnet

> Test-gating for .NET — skip tests dynamically via a Google Spreadsheet, without touching your test code.

[![NuGet](https://img.shields.io/nuget/v/GetSkipper.Core)](https://www.nuget.org/profiles/get-skipper)

## What is Skipper?

Skipper reads a Google Spreadsheet to decide which tests to skip. Each row has:

| testId | disabledUntil | notes |
|---|---|---|
| `Tests/Unit/AuthTests.cs > AuthTests > CanLogin` | `2026-06-01` | Flaky on CI |
| `Tests/E2E/LoginTests.cs > LoginTests > UserCanLogin` | | |

- **`disabledUntil` is in the future** → test is skipped automatically
- **`disabledUntil` is empty or in the past** → test runs normally
- **Test not in spreadsheet** → test runs normally (opt-out model)

No code changes required in your tests. Configure once, gate everywhere.

---

## Packages

| Package | Description | NuGet |
|---|---|---|
| [`GetSkipper.Core`](src/GetSkipper.Core/) | Google Sheets client, resolver, writer | [![NuGet](https://img.shields.io/nuget/v/GetSkipper.Core)](https://www.nuget.org/packages/GetSkipper.Core) |
| [`GetSkipper.XUnit`](src/GetSkipper.XUnit/) | xUnit v2/v3 integration | [![NuGet](https://img.shields.io/nuget/v/GetSkipper.XUnit)](https://www.nuget.org/packages/GetSkipper.XUnit) |
| [`GetSkipper.NUnit`](src/GetSkipper.NUnit/) | NUnit 3/4 integration | [![NuGet](https://img.shields.io/nuget/v/GetSkipper.NUnit)](https://www.nuget.org/packages/GetSkipper.NUnit) |
| [`GetSkipper.MSTest`](src/GetSkipper.MSTest/) | MSTest v3 integration | [![NuGet](https://img.shields.io/nuget/v/GetSkipper.MSTest)](https://www.nuget.org/packages/GetSkipper.MSTest) |
| [`GetSkipper.Playwright`](src/GetSkipper.Playwright/) | Playwright for .NET | [![NuGet](https://img.shields.io/nuget/v/GetSkipper.Playwright)](https://www.nuget.org/packages/GetSkipper.Playwright) |
| [`GetSkipper.SpecFlow`](src/GetSkipper.SpecFlow/) | Reqnroll (SpecFlow) BDD | [![NuGet](https://img.shields.io/nuget/v/GetSkipper.SpecFlow)](https://www.nuget.org/packages/GetSkipper.SpecFlow) |

---

## Quick Start

### xUnit (zero changes to your tests)

```csharp
// AssemblyInfo.cs — add once to your test project
using GetSkipper.XUnit;

[assembly: TestFramework("GetSkipper.XUnit.SkipperTestFramework", "GetSkipper.XUnit")]
[assembly: SkipperConfig(
    SpreadsheetIdEnvVar = "SKIPPER_SPREADSHEET_ID",
    CredentialsFile = "service-account.json")]

// Your tests remain unchanged:
public class AuthTests
{
    [Fact]
    public void CanLogin() { /* ... */ }
}
```

### NUnit (zero changes to your tests)

```csharp
// AssemblyInfo.cs
using GetSkipper.NUnit;

[assembly: SkipperConfig(
    SpreadsheetId = "1abc...",
    CredentialsFile = "service-account.json")]

// Your tests remain unchanged:
[TestFixture]
public class AuthTests
{
    [Test]
    public void CanLogin() { /* ... */ }
}
```

### MSTest (zero changes to your tests)

```csharp
// SkipperSetup.cs — add once to your test project
using GetSkipper.Core;
using GetSkipper.Core.Credentials;
using GetSkipper.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public static class SkipperSetup
{
    [GlobalTestInitialize]
    public static async Task InitAsync()
        => await SkipperGlobalHooks.ConfigureAsync(new SkipperConfig
        {
            SpreadsheetId = "1abc...",
            Credentials = new FileCredentials("service-account.json"),
        });

    [GlobalTestCleanup]
    public static Task CleanupAsync() => SkipperGlobalHooks.AfterAllTestsAsync();
}

// Intercept each test (call from a second [GlobalTestInitialize] or use one method):
public static class SkipperTestGate
{
    [GlobalTestInitialize]
    public static void BeforeTest(TestContext ctx) => SkipperGlobalHooks.BeforeTest(ctx);
}
```

### Playwright (change base class only)

```csharp
using GetSkipper.NUnit;
using GetSkipper.Playwright;

[assembly: SkipperConfig(SpreadsheetId = "1abc...", CredentialsFile = "service-account.json")]

[TestFixture]
public class LoginTests : SkipperPageTest  // was: PageTest
{
    [Test]
    public async Task UserCanLogin()
    {
        await Page.GotoAsync("/login");
        // automatic skip is handled before this line
    }
}
```

### Reqnroll / SpecFlow (zero changes to step definitions)

```json
// reqnroll.json
{
  "bindingAssemblies": [
    { "assembly": "GetSkipper.SpecFlow" }
  ]
}
```

```bash
SKIPPER_SPREADSHEET_ID=1abc... SKIPPER_CREDENTIALS_FILE=./service-account.json dotnet test
```

---

## Spreadsheet Format

| Column | Required | Description |
|---|---|---|
| `testId` | Yes | Canonical test identifier |
| `disabledUntil` | No | ISO-8601 date (`2026-06-01`). Empty = enabled |
| `notes` | No | Free text |

### Test ID formats

| Framework | Format |
|---|---|
| xUnit / NUnit / MSTest | `Tests/Unit/AuthTests.cs > AuthTests > CanLogin` |
| Playwright | `Tests/E2E/LoginTests.cs > LoginTests > UserCanLogin` |
| Reqnroll | `Features/Auth/Login.feature > User authentication > User can log in` |

---

## Sync Mode

Reconcile the spreadsheet with your test suite (append new tests, remove deleted ones):

```bash
SKIPPER_MODE=sync dotnet test
```

Typically run on `main` after all tests pass:

```yaml
# .github/workflows/ci.yml
- name: Run tests in sync mode
  if: github.ref == 'refs/heads/main'
  env:
    SKIPPER_MODE: sync
    SKIPPER_SPREADSHEET_ID: ${{ secrets.SKIPPER_SPREADSHEET_ID }}
    GOOGLE_CREDS_B64: ${{ secrets.GOOGLE_CREDS_B64 }}
  run: dotnet test
```

---

## Credentials

Three ways to supply the Google service account:

```csharp
// 1. JSON file (local dev)
Credentials = new FileCredentials("./service-account.json")

// 2. Base-64 encoded JSON (CI secrets)
Credentials = new Base64Credentials(Environment.GetEnvironmentVariable("GOOGLE_CREDS_B64")!)

// 3. Inline object
Credentials = new ServiceAccountCredentials(
    clientEmail: "skipper@project.iam.gserviceaccount.com",
    privateKey: "-----BEGIN RSA PRIVATE KEY-----\n...")
```

---

## Environment Variables

| Variable | Default | Description |
|---|---|---|
| `SKIPPER_MODE` | `read-only` | Set to `sync` to reconcile spreadsheet |
| `SKIPPER_SPREADSHEET_ID` | — | Spreadsheet ID (used with `SpreadsheetIdEnvVar`) |
| `GOOGLE_CREDS_B64` | — | Base-64 credentials (used with `CredentialsEnvVar`) |
| `SKIPPER_DEBUG` | — | Set to any value for verbose logging |
| `SKIPPER_FAIL_OPEN` | `true` | When `true`, an API failure falls back to the local cache (if valid) or runs all tests. Set to `false` to restore the original crash behaviour. |
| `SKIPPER_CACHE_TTL` | `300` | Seconds a local `.skipper-cache.json` file remains valid as a fallback after an API failure. |
| `SKIPPER_SYNC_ALLOW_DELETE` | `false` | In `sync` mode, orphaned rows are only deleted when this is `true`. By default they are warned about but left untouched. |

---

## License

MIT — see [LICENSE](LICENSE).
