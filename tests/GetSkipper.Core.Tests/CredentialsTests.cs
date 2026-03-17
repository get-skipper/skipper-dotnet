using System.Text;
using System.Text.Json;
using GetSkipper.Core.Credentials;
using Xunit;

namespace GetSkipper.Core.Tests;

public sealed class CredentialsTests
{
    [Fact]
    public void FileCredentials_ThrowsWhenFileNotFound()
    {
        var creds = new FileCredentials("/nonexistent/path.json");
        Assert.Throws<FileNotFoundException>(() => creds.Resolve());
    }

    [Fact]
    public void FileCredentials_ReadsJsonFile()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """{"type":"service_account","project_id":"test"}""");
            var creds = new FileCredentials(path);
            var result = creds.Resolve();
            Assert.Equal("service_account", result["type"].ToString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Base64Credentials_DecodesJson()
    {
        var json = """{"type":"service_account"}""";
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        var creds = new Base64Credentials(b64);
        var result = creds.Resolve();
        Assert.Equal("service_account", result["type"].ToString());
    }

    [Fact]
    public void Base64Credentials_ThrowsOnInvalidBase64()
    {
        var creds = new Base64Credentials("!!!not-base64!!!");
        Assert.Throws<InvalidOperationException>(() => creds.Resolve());
    }

    [Fact]
    public void ServiceAccountCredentials_ReturnsExpectedDictionary()
    {
        var creds = new ServiceAccountCredentials(
            clientEmail: "bot@project.iam.gserviceaccount.com",
            privateKey: "-----BEGIN RSA PRIVATE KEY-----\nfake\n-----END RSA PRIVATE KEY-----");

        var result = creds.Resolve();
        Assert.Equal("service_account", result["type"].ToString());
        Assert.Equal("bot@project.iam.gserviceaccount.com", result["client_email"].ToString());
    }
}
