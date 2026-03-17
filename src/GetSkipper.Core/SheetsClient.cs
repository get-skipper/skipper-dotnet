using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;

namespace GetSkipper.Core;

/// <summary>Result of fetching a single sheet tab.</summary>
public sealed class SheetFetchResult(
    string sheetName,
    int sheetId,
    IList<IList<object>> rawRows,
    IList<string> header,
    IList<TestEntry> entries)
{
    public string SheetName { get; } = sheetName;
    public int SheetId { get; } = sheetId;
    public IList<IList<object>> RawRows { get; } = rawRows;
    public IList<string> Header { get; } = header;
    public IList<TestEntry> Entries { get; } = entries;
}

/// <summary>
/// Combined result of fetching the primary sheet and all reference sheets.
/// The <see cref="Entries"/> collection is deduplicated: when the same test ID appears
/// in multiple sheets, the most restrictive (latest) <c>disabledUntil</c> date wins.
/// </summary>
public sealed class FetchAllResult(
    SheetFetchResult primary,
    IList<TestEntry> entries,
    SheetsService service)
{
    public SheetFetchResult Primary { get; } = primary;

    /// <summary>Merged, deduplicated entries from all sheets.</summary>
    public IList<TestEntry> Entries { get; } = entries;

    /// <summary>
    /// Authenticated <see cref="SheetsService"/> instance.
    /// Reused by <see cref="SheetsWriter"/> to avoid re-authenticating.
    /// </summary>
    public SheetsService Service { get; } = service;
}

/// <summary>
/// Reads test entries from Google Sheets using a service account.
/// </summary>
public sealed class SheetsClient(SkipperConfig config)
{
    /// <summary>Fetches the primary sheet and all reference sheets, then merges the results.</summary>
    public async Task<FetchAllResult> FetchAllAsync(CancellationToken ct = default)
    {
        var service = await BuildServiceAsync(ct);
        var spreadsheetId = config.SpreadsheetId;

        SkipperLogger.Log($"Fetching spreadsheet metadata for {spreadsheetId}");

        var metaRequest = service.Spreadsheets.Get(spreadsheetId);
        var meta = await metaRequest.ExecuteAsync(ct);

        var sheetIdByName = meta.Sheets
            .Where(s => s.Properties?.Title != null)
            .ToDictionary(
                s => s.Properties.Title,
                s => (int)(s.Properties.SheetId ?? 0));

        var primaryName = config.SheetName
            ?? meta.Sheets.FirstOrDefault()?.Properties?.Title
            ?? "Sheet1";

        if (!sheetIdByName.TryGetValue(primaryName, out var primarySheetId))
            throw new InvalidOperationException(
                $"[skipper] Sheet \"{primaryName}\" not found. Available sheets: {string.Join(", ", sheetIdByName.Keys)}");

        SkipperLogger.Log($"Fetching primary sheet \"{primaryName}\"");
        var primary = await FetchSheetAsync(service, primaryName, primarySheetId, ct);

        var referenceEntries = new List<TestEntry>();
        foreach (var refName in config.ReferenceSheets)
        {
            if (!sheetIdByName.TryGetValue(refName, out var refSheetId))
            {
                SkipperLogger.Warn($"Reference sheet \"{refName}\" not found — skipping.");
                continue;
            }
            SkipperLogger.Log($"Fetching reference sheet \"{refName}\"");
            var refResult = await FetchSheetAsync(service, refName, refSheetId, ct);
            referenceEntries.AddRange(refResult.Entries);
        }

        // Merge: most restrictive (latest) disabledUntil wins
        var merged = new Dictionary<string, TestEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in primary.Entries.Concat(referenceEntries))
        {
            var key = TestIdHelper.Normalize(entry.TestId);
            if (!merged.TryGetValue(key, out var existing))
            {
                merged[key] = entry;
            }
            else if (entry.DisabledUntil.HasValue)
            {
                if (!existing.DisabledUntil.HasValue || entry.DisabledUntil > existing.DisabledUntil)
                    merged[key] = entry;
            }
        }

        SkipperLogger.Log($"Loaded {merged.Count} test entries total.");
        return new FetchAllResult(primary, [.. merged.Values], service);
    }

    private async Task<SheetFetchResult> FetchSheetAsync(
        SheetsService service, string sheetName, int sheetId, CancellationToken ct)
    {
        var spreadsheetId = config.SpreadsheetId;
        var testIdCol = config.TestIdColumn;
        var disabledUntilCol = config.DisabledUntilColumn;

        var request = service.Spreadsheets.Values.Get(spreadsheetId, sheetName);
        var response = await request.ExecuteAsync(ct);

        var rawRows = response.Values ?? [];
        if (rawRows.Count == 0)
            return new SheetFetchResult(sheetName, sheetId, rawRows, [], []);

        var header = rawRows[0]
            .Select(h => h?.ToString()?.Trim() ?? string.Empty)
            .ToList();

        var testIdIdx = header.IndexOf(testIdCol);
        if (testIdIdx < 0)
            throw new InvalidOperationException(
                $"[skipper] Column \"{testIdCol}\" not found in sheet \"{sheetName}\". Found: {string.Join(", ", header)}");

        var disabledUntilIdx = header.IndexOf(disabledUntilCol);
        var notesIdx = header.IndexOf("notes");

        var entries = new List<TestEntry>();
        for (var i = 1; i < rawRows.Count; i++)
        {
            var row = rawRows[i];

            var testId = testIdIdx < row.Count
                ? row[testIdIdx]?.ToString()?.Trim()
                : null;

            if (string.IsNullOrEmpty(testId)) continue;

            DateTimeOffset? disabledUntil = null;
            if (disabledUntilIdx >= 0 && disabledUntilIdx < row.Count)
            {
                var raw = row[disabledUntilIdx]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(raw))
                {
                    if (DateTimeOffset.TryParse(raw, out var parsed))
                        disabledUntil = parsed;
                    else
                        SkipperLogger.Warn(
                            $"Row {i + 1} in \"{sheetName}\": invalid date \"{raw}\" — treating test as enabled.");
                }
            }

            var notes = notesIdx >= 0 && notesIdx < row.Count
                ? row[notesIdx]?.ToString()
                : null;

            entries.Add(new TestEntry(testId, disabledUntil, notes));
        }

        SkipperLogger.Log($"Sheet \"{sheetName}\": {entries.Count} entries parsed.");
        return new SheetFetchResult(sheetName, sheetId, rawRows, header, entries);
    }

    internal async Task<SheetsService> BuildServiceAsync(CancellationToken ct = default)
    {
        var credDict = config.Credentials.Resolve();
        var credJson = JsonSerializer.Serialize(credDict);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(credJson));

        var credential = await GoogleCredential.FromStreamAsync(stream, ct);
        credential = credential.CreateScoped(SheetsService.Scope.Spreadsheets);

        return new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "skipper-dotnet",
        });
    }
}
