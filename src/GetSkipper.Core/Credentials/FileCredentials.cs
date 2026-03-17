using System.Text.Json;

namespace GetSkipper.Core.Credentials;

/// <summary>
/// Reads Google service account credentials from a JSON file on disk.
/// Suitable for local development.
/// </summary>
/// <param name="credentialsFile">Absolute or relative path to the service account JSON file.</param>
public sealed class FileCredentials(string credentialsFile) : ISkipperCredentials
{
    public Dictionary<string, object> Resolve()
    {
        if (!File.Exists(credentialsFile))
            throw new FileNotFoundException(
                $"[skipper] Credentials file not found: {credentialsFile}");

        var json = File.ReadAllText(credentialsFile);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json)
            ?? throw new InvalidDataException(
                "[skipper] Credentials file is empty or contains invalid JSON.");
    }
}
