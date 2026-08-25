using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using VIBN_Tools.Core.ViCo;

namespace VIBN_Tools.Infrastructure.ViCo;

/// <summary>
/// Read-only compatibility reader for assignments written by the former ViCo
/// licensing implementation.  It exists only for the one-time migration to
/// <c>roles.json</c>; the application never writes or processes license
/// requests.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class LegacyRoleMigrationReader
{
    private readonly string _assignmentsRoot;
    private readonly string _key;

    public LegacyRoleMigrationReader(string assignmentsRoot, string? key)
    {
        _assignmentsRoot = assignmentsRoot ?? string.Empty;
        _key = key ?? string.Empty;
    }

    public bool CanRead => Encoding.UTF8.GetByteCount(_key) is 16 or 24 or 32;

    public async Task<IReadOnlyList<ViCoUserRole>> LoadAsync(CancellationToken cancellationToken = default)
    {
        EnsureCanRead();
        if (!Directory.Exists(_assignmentsRoot))
            return Array.Empty<ViCoUserRole>();

        var result = new List<ViCoUserRole>();
        foreach (var file in Directory.EnumerateFiles(_assignmentsRoot, "*.txt", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var lines = await File.ReadAllLinesAsync(file, cancellationToken);
                if (lines.Length < 2)
                    continue;

                result.Add(new ViCoUserRole(
                    Decrypt(lines[0]),
                    Decrypt(lines[1]),
                    "historische Rollenmigration"));
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or CryptographicException or FormatException)
            {
                // A corrupt predecessor entry must not prevent valid role
                // assignments from being migrated.
            }
        }

        return result
            .OrderBy(role => role.UserName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private string Decrypt(string value)
    {
        using var aes = CreateAes();
        using var input = new MemoryStream(Convert.FromBase64String(value));
        using var crypto = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var reader = new StreamReader(crypto);
        return reader.ReadToEnd();
    }

    private Aes CreateAes()
    {
        EnsureCanRead();
        var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_key);
        aes.IV = new byte[16];
        return aes;
    }

    private void EnsureCanRead()
    {
        if (!CanRead)
            throw new InvalidOperationException("Die historische Rollenmigration ist nicht konfiguriert.");
    }
}
