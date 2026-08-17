using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Runtime.Versioning;
using VIBN_Tools.Core.ViCo;

namespace VIBN_Tools.Infrastructure.ViCo;

[SupportedOSPlatform("windows")]
public sealed class LegacyLicenseService : IViCoLicenseService
{
    private readonly string _approvedRoot;
    private readonly string _requestRoot;
    private readonly string _key;

    public LegacyLicenseService(string approvedRoot, string requestRoot, string? key)
    {
        _approvedRoot = approvedRoot;
        _requestRoot = requestRoot;
        _key = key ?? string.Empty;
    }

    public bool IsConfigured => Encoding.UTF8.GetByteCount(_key) is 16 or 24 or 32;

    public Task<IReadOnlyList<ViCoLicenseEntry>> LoadApprovedAsync(CancellationToken cancellationToken = default) =>
        LoadAsync(_approvedRoot, requested: false, cancellationToken);

    public Task<IReadOnlyList<ViCoLicenseEntry>> LoadRequestsAsync(CancellationToken cancellationToken = default) =>
        LoadAsync(_requestRoot, requested: true, cancellationToken);

    public async Task RequestCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var userName = WindowsIdentity.GetCurrent().Name.ToLowerInvariant();
        Directory.CreateDirectory(_requestRoot);
        var encrypted = Encrypt(userName);
        var fileName = SanitizeFileName(encrypted) + ".txt";
        await File.WriteAllTextAsync(Path.Combine(_requestRoot, fileName), encrypted, cancellationToken);
    }

    public async Task SetLevelAsync(string userName, string level, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(level))
            throw new ArgumentException("User name and license level are required.");

        Directory.CreateDirectory(_approvedRoot);
        var encryptedName = Encrypt(userName.ToLowerInvariant());
        var destination = Path.Combine(_approvedRoot, SanitizeFileName(encryptedName) + ".txt");
        await File.WriteAllLinesAsync(destination, new[] { encryptedName, Encrypt(level) }, cancellationToken);
    }

    private async Task<IReadOnlyList<ViCoLicenseEntry>> LoadAsync(
        string root,
        bool requested,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        if (!Directory.Exists(root))
            return Array.Empty<ViCoLicenseEntry>();

        var result = new List<ViCoLicenseEntry>();
        foreach (var file in Directory.EnumerateFiles(root, "*.txt", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var lines = await File.ReadAllLinesAsync(file, cancellationToken);
                if (lines.Length == 0)
                    continue;
                result.Add(new ViCoLicenseEntry(
                    Decrypt(lines[0]),
                    requested || lines.Length < 2 ? "Requested" : Decrypt(lines[1]),
                    file));
            }
            catch (Exception exception) when (exception is IOException or CryptographicException or FormatException)
            {
                // Invalid legacy entries are skipped while valid users remain available.
            }
        }

        return result.OrderBy(entry => entry.UserName, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private string Encrypt(string value)
    {
        using var aes = CreateAes();
        using var output = new MemoryStream();
        using (var crypto = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write))
        using (var writer = new StreamWriter(crypto))
            writer.Write(value);
        return Convert.ToBase64String(output.ToArray());
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
        EnsureConfigured();
        var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_key);
        aes.IV = new byte[16];
        return aes;
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new InvalidOperationException("ViCo license compatibility key is not configured.");
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        return new string(value.Select(character => invalid.Contains(character) ? '_' : character).ToArray())
            .Trim()
            .TrimEnd('.');
    }
}
