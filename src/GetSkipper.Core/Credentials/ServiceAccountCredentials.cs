namespace GetSkipper.Core.Credentials;

/// <summary>
/// Inline Google service account credentials object.
/// Use when you want to supply individual fields directly in code
/// (e.g., loaded from your own configuration system).
/// </summary>
public sealed class ServiceAccountCredentials(
    string clientEmail,
    string privateKey,
    string projectId = "",
    string privateKeyId = "",
    string clientId = "",
    string tokenUri = "https://oauth2.googleapis.com/token") : ISkipperCredentials
{
    public Dictionary<string, object> Resolve() => new()
    {
        ["type"] = "service_account",
        ["project_id"] = projectId,
        ["private_key_id"] = privateKeyId,
        ["private_key"] = privateKey,
        ["client_email"] = clientEmail,
        ["client_id"] = clientId,
        ["auth_uri"] = "https://accounts.google.com/o/oauth2/auth",
        ["token_uri"] = tokenUri,
    };
}
