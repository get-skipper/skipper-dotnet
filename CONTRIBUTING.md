# Contributing to skipper-dotnet

Thank you for your interest in contributing!

---

## Requirements

- .NET 8.0 SDK
- A Google Cloud service account (for integration tests against a real spreadsheet)

## Setup

```bash
git clone https://github.com/get-skipper/skipper-dotnet.git
cd skipper-dotnet
dotnet restore
```

## Running tests

```bash
# Unit tests only
dotnet test --filter "FullyQualifiedName!~Integration"

# Integration tests (requires real credentials)
SKIPPER_SPREADSHEET_ID=1abc... GOOGLE_CREDS_B64=... dotnet test tests/GetSkipper.Integration.Tests

# Sync mode integration test
SKIPPER_MODE=sync SKIPPER_SPREADSHEET_ID=1abc... GOOGLE_CREDS_B64=... dotnet test tests/GetSkipper.Integration.Tests
```

## Building

```bash
dotnet build skipper-dotnet.sln
```

## Packing (NuGet)

```bash
dotnet pack src/GetSkipper.Core -c Release
```

---

## Commit messages

All commits **must** follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:

```
type(scope): short description

[optional body]

[optional footer]
```

### Types

| Type | When to use |
|---|---|
| `feat` | A new feature or integration |
| `fix` | A bug fix |
| `docs` | Documentation changes only |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `test` | Adding or updating tests |
| `chore` | Build process, dependency updates, tooling |

### Scopes

| Scope | Applies to |
|---|---|
| `core` | `src/GetSkipper.Core/` |
| `xunit` | `src/GetSkipper.XUnit/` |
| `nunit` | `src/GetSkipper.NUnit/` |
| `mstest` | `src/GetSkipper.MSTest/` |
| `playwright` | `src/GetSkipper.Playwright/` |
| `specflow` | `src/GetSkipper.SpecFlow/` |

### Examples

```
feat(xunit): add support for parallel worker cache sharing
fix(nunit): correct test ID format for parameterised tests
docs(playwright): document SkipperPageTest usage
refactor(core): extract SheetsClient authentication into separate method
test(core): add edge cases for TestIdHelper.Normalize
chore: update Google.Apis.Sheets.v4 to latest
```

---

## Pull requests

1. Fork the repository and create a branch:
   ```bash
   git checkout -b feat/my-feature
   ```

2. Make your changes. Ensure:
   - `dotnet build` succeeds with no warnings
   - `dotnet test --filter "FullyQualifiedName!~Integration"` passes
   - New functionality has corresponding unit tests

3. Commit using Conventional Commits format (see above).

4. Open a pull request with a descriptive title.

5. PRs are merged via **squash merge** into `main`. The squash commit must follow the conventional commit format.

## Rules

- No direct commits to `main`
- No `--no-verify` to bypass hooks
- All CI checks must pass before merge
- Keep PRs focused — one feature or fix per PR

---

## Project structure

```
src/
├── GetSkipper.Core/       # Google Sheets client, resolver, writer, models
├── GetSkipper.XUnit/      # xUnit v2/v3 integration (custom ITestFramework)
├── GetSkipper.NUnit/      # NUnit 3/4 integration (TestActionAttribute at assembly level)
├── GetSkipper.MSTest/     # MSTest v3 integration ([GlobalTestInitialize])
├── GetSkipper.Playwright/ # Playwright for .NET (SkipperPageTest base classes)
└── GetSkipper.SpecFlow/   # Reqnroll/SpecFlow BDD ([BeforeScenario] binding)
tests/
├── GetSkipper.Core.Tests/
├── GetSkipper.XUnit.Tests/
├── GetSkipper.NUnit.Tests/
├── GetSkipper.MSTest.Tests/
├── GetSkipper.Playwright.Tests/
├── GetSkipper.SpecFlow.Tests/
└── GetSkipper.Integration.Tests/  # self-test with real Google Sheets
```

Each framework integration must:
- Initialise the resolver (or rehydrate from cache) before tests run
- Skip disabled tests using the framework's native skip mechanism (`Assert.Skip`, `Assert.Ignore`, `Assert.Inconclusive`, `scenarioContext.Pending()`)
- Collect discovered test IDs for sync mode
- Call `SheetsWriter.SyncAsync()` after all tests finish (sync mode only)

See existing integrations for reference patterns.
