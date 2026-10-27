using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Logiwall.IntegrationTests.Infrastructure;

/// <summary>
/// Writes a fresh self-signed IdentityCertificate.pfx into the test output folder.
/// The committed development certificate has expired, and token validation rejects
/// signing keys whose certificate has expired (IDX10249).
/// </summary>
public static class TestCertificate
{
    public const string Password = "Test-Cert-Pass-1";

    public static void WriteTo(string directory)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Logiwall.IntegrationTests", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        File.WriteAllBytes(Path.Combine(directory, "IdentityCertificate.pfx"), certificate.Export(X509ContentType.Pfx, Password));
    }
}
