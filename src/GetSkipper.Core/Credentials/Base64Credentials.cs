using System.Text;
using System.Text.Json;

namespace GetSkipper.Core.Credentials;

/// <summary>
/// Decodes base-64 encoded Google service account JSON.
/// Suitable for CI environments where the JSON is stored as a secret.
/// </summary>
/// <param name="credentialsBase64">Base-64 string of the service account JSON.</param>
public sealed class Base64Credentials(string credentialsBase64) : ISkipperCredentials
{
    public Dictionary<string, object> Resolve()
    {
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(credentialsBase64);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                "[skipper] Failed to base64-decode credentials string.", ex);
        }

        var json = Encoding.UTF8.GetString(bytes);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json)
            ?? throw new InvalidDataException(
                "[skipper] Decoded credentials are empty or contain invalid JSON.");
    }
}
