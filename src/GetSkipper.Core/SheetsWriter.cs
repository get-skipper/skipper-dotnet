using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace GetSkipper.Core;

/// <summary>
/// Reconciles the primary sheet with the set of test IDs discovered during a test run.
/// <list type="bullet">
///   <item>Appends rows for test IDs that are not yet in the sheet (with empty <c>disabledUntil</c>).</item>
///   <item>Deletes rows for test IDs that are in the sheet but no longer discovered, when
///   <c>SKIPPER_SYNC_ALLOW_DELETE=true</c> (default: <c>false</c>).</item>
///   <item>Never modifies reference sheets.</item>
/// </list>
/// </summary>
public sealed class SheetsWriter(SkipperConfig config, SheetsService service)
{
    /// <summary>For unit-testing only — reads the SKIPPER_SYNC_ALLOW_DELETE env var.</summary>
    internal static bool ReadAllowDeleteForTest()
    {
        var raw = Environment.GetEnvironmentVariable("SKIPPER_SYNC_ALLOW_DELETE") ?? string.Empty;
        return string.Equals(raw.Trim(), "true", StringComparison.OrdinalIgnoreCase)
            || raw.Trim() == "1";
    }

    /// <summary>
    /// Reconciles the spreadsheet with <paramref name="discoveredTestIds"/>.
    /// Should be called once, after all tests have run.
    /// </summary>
    public async Task SyncAsync(IEnumerable<string> discoveredTestIds, CancellationToken ct = default)
    {
        var spreadsheetId = config.SpreadsheetId;

        // Re-fetch the primary sheet to get current state
        var client = new SheetsClient(config);
        var fetchResult = await client.FetchAllAsync(ct);
        var primary = fetchResult.Primary;
        var sheetName = primary.SheetName;
        var sheetId = primary.SheetId;

        var testIdCol = config.TestIdColumn;
        var header = primary.Header;
        var testIdColIdx = header.IndexOf(testIdCol);

        var existingIds = primary.Entries
            .Select(e => TestIdHelper.Normalize(e.TestId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var discovered = discoveredTestIds
            .Select(TestIdHelper.Normalize)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // New IDs: in discovered but not in sheet
        var toAdd = discovered.Except(existingIds).ToList();

        // Stale IDs: in sheet but not in discovered
        var staleNormalized = existingIds.Except(discovered).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (toAdd.Count == 0 && staleNormalized.Count == 0)
        {
            SkipperLogger.Log("Sync: spreadsheet is already up to date.");
            return;
        }

        // --- Delete stale rows (in descending order to avoid index shifting) ---
        //
        // SKIPPER_SYNC_ALLOW_DELETE (default: false) — orphaned rows are only deleted when
        // explicitly opted in, preventing accidental data loss.
        if (staleNormalized.Count > 0)
        {
            var raw = Environment.GetEnvironmentVariable("SKIPPER_SYNC_ALLOW_DELETE") ?? string.Empty;
            var allowDelete = string.Equals(raw.Trim(), "true", StringComparison.OrdinalIgnoreCase)
                || raw.Trim() == "1";

            if (!allowDelete)
            {
                SkipperLogger.Warn(
                    $"Sync: {staleNormalized.Count} orphaned row(s) would be deleted but " +
                    "SKIPPER_SYNC_ALLOW_DELETE is not set — skipping deletion. " +
                    "Set SKIPPER_SYNC_ALLOW_DELETE=true to enable.");
                staleNormalized.Clear();
            }
        }

        if (staleNormalized.Count > 0)
        {
            var rawRows = primary.RawRows;
            var rowsToDelete = new List<int>();

            for (var i = 1; i < rawRows.Count; i++)
            {
                var row = rawRows[i];
                var cellId = testIdColIdx < row.Count
                    ? TestIdHelper.Normalize(row[testIdColIdx]?.ToString() ?? string.Empty)
                    : string.Empty;

                if (staleNormalized.Contains(cellId))
                    rowsToDelete.Add(i); // 0-based index in rawRows (row 0 = header)
            }

            // Delete in descending order so earlier indices stay valid
            rowsToDelete.Sort();
            rowsToDelete.Reverse();

            var deleteRequests = rowsToDelete.Select(rowIdx => new Request
            {
                DeleteDimension = new DeleteDimensionRequest
                {
                    Range = new DimensionRange
                    {
                        SheetId = sheetId,
                        Dimension = "ROWS",
                        StartIndex = rowIdx,     // 0-based, inclusive
                        EndIndex = rowIdx + 1,   // exclusive
                    }
                }
            }).ToList();

            var batchDelete = new BatchUpdateSpreadsheetRequest { Requests = deleteRequests };
            await service.Spreadsheets.BatchUpdate(batchDelete, spreadsheetId).ExecuteAsync(ct);
            SkipperLogger.Log($"Sync: deleted {deleteRequests.Count} stale row(s).");
        }

        // --- Append new rows ---
        if (toAdd.Count > 0)
        {
            // Build value range: one row per new test ID
            var values = toAdd.Select(id =>
            {
                // Fill columns up to testIdColIdx, put the ID, leave disabledUntil blank
                var row = new List<object>(new object[testIdColIdx + 1]);
                row[testIdColIdx] = id;
                return (IList<object>)row;
            }).ToList();

            var appendRequest = service.Spreadsheets.Values.Append(
                new ValueRange { Values = values },
                spreadsheetId,
                sheetName);

            appendRequest.ValueInputOption =
                SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

            await appendRequest.ExecuteAsync(ct);
            SkipperLogger.Log($"Sync: appended {toAdd.Count} new test ID(s).");
        }
    }
}
