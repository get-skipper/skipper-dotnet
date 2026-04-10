# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-04-10

### Added

- **`SKIPPER_FAIL_OPEN`** environment variable: when set to `true`, Skipper skips all tests if it cannot reach the Google Sheets backend (fail-open mode). Defaults to `false` (fail-closed).
- **`SKIPPER_CACHE_TTL`** environment variable: controls how long (in seconds) the local cache of the sheet is considered valid before a refresh is triggered.
- **`SKIPPER_SYNC_ALLOW_DELETE`** environment variable: when set to `true`, allows the sync step to delete rows from the sheet that no longer correspond to any known test.
- MIT LICENSE file.

### Fixed

- Date parsing for `disabledUntil` is now strictly UTC-pinned to avoid timezone-dependent behaviour across different CI environments.
- JSON serialization uses `UnsafeRelaxedJsonEscaping` and an explicit null check to prevent encoding issues with test names containing special characters.

## [1.0.0] - 2026-03-01

### Added

- Initial release with support for xUnit, NUnit, MSTest, Playwright, and SpecFlow.
- Google Sheets backend integration via `SKIPPER_CREDENTIALS`, `SKIPPER_SPREADSHEET_ID`, and `SKIPPER_SHEET_NAME`.
- `SKIPPER_SHEET_NAME` environment variable to override the default sheet name.
- Fail-open behaviour when Skipper is not configured (xUnit adapter).
- Backslash normalisation in test IDs for cross-platform consistency.
