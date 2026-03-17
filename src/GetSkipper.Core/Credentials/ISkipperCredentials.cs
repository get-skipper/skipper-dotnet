namespace GetSkipper.Core.Credentials;

/// <summary>
/// Abstraction over the three supported credential sources:
/// a JSON file on disk, a base-64 encoded string (for CI), or an inline object.
/// </summary>
public interface ISkipperCredentials
{
    /// <summary>
    /// Resolves the service account key as a dictionary ready to be serialised
    /// and passed to <see cref="Google.Apis.Auth.OAuth2.GoogleCredential"/>.
    /// </summary>
    Dictionary<string, object> Resolve();
}
