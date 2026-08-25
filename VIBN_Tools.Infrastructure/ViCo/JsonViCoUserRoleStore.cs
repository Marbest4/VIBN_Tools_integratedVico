using System.Text.Json;
using VIBN_Tools.Core.ViCo;

namespace VIBN_Tools.Infrastructure.ViCo;

/// <summary>
/// Central JSON-backed role store.  The persisted document contains only
/// Windows user names and tool levels; it intentionally has no license,
/// request or expiry data.
/// </summary>
public sealed class JsonViCoUserRoleStore : IViCoUserRoleStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _rolesFile;
    private readonly Func<CancellationToken, Task<IReadOnlyList<ViCoUserRole>>>? _legacyMigration;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <param name="rolesFile">Shared roles.json file.</param>
    /// <param name="legacyMigration">
    /// Optional one-time reader for the predecessor's encrypted assignments.
    /// It is invoked only when roles.json does not yet exist and never writes
    /// the old format back.
    /// </param>
    public JsonViCoUserRoleStore(
        string rolesFile,
        Func<CancellationToken, Task<IReadOnlyList<ViCoUserRole>>>? legacyMigration = null)
    {
        _rolesFile = rolesFile?.Trim() ?? string.Empty;
        _legacyMigration = legacyMigration;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_rolesFile);

    public async Task<IReadOnlyList<ViCoUserRole>> LoadAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(_rolesFile))
                return await ReadFileAsync(cancellationToken);

            if (_legacyMigration is null)
                return ViCoRolePolicy.ApplyMandatoryRoles(Array.Empty<ViCoUserRole>());

            var migrated = ViCoRolePolicy.ApplyMandatoryRoles(await _legacyMigration(cancellationToken));
            var migrationPlan = ViCoRolePolicy.PlanSave(migrated);
            if (migrationPlan.IsValid)
                await WriteFileAsync(migrationPlan.Roles, cancellationToken);

            // If historical data has fewer than two administrators, do not
            // persist a policy violation. lutzma can open the administration
            // page and add the required second Level9 user first.
            return migrated;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(
        IReadOnlyCollection<ViCoUserRole> roles,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        ArgumentNullException.ThrowIfNull(roles);
        var plan = ViCoRolePolicy.PlanSave(roles);
        if (!plan.IsValid)
            throw new InvalidOperationException(plan.Message);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await WriteFileAsync(plan.Roles, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IReadOnlyList<ViCoUserRole>> ReadFileAsync(CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            _rolesFile,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        var document = await JsonSerializer.DeserializeAsync<RoleStoreDocument>(stream, JsonOptions, cancellationToken)
            ?? new RoleStoreDocument();
        return ViCoRolePolicy.ApplyMandatoryRoles(
            document.Roles.Select(role => new ViCoUserRole(role.UserName, role.Level, _rolesFile)));
    }

    private async Task WriteFileAsync(
        IReadOnlyList<ViCoUserRole> roles,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_rolesFile);
        if (string.IsNullOrWhiteSpace(directory))
            throw new InvalidOperationException("Der Rollenpfad enthält kein Verzeichnis.");

        Directory.CreateDirectory(directory);
        var document = new RoleStoreDocument
        {
            Roles = roles
                .Select(role => new RoleStoreEntry
                {
                    UserName = WindowsUserIdentity.Normalize(role.UserName),
                    Level = ViCoRolePolicy.NormalizeLevel(role.Level)
                })
                .Where(role => role.UserName.Length > 0)
                .OrderBy(role => role.UserName, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };

        var temporary = _rolesFile + "." + Guid.NewGuid().ToString("N") + ".tmp";
        await using (var stream = new FileStream(
            temporary,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 16 * 1024,
            useAsync: true))
        {
            await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        // Replace is atomic within the shared folder and prevents readers from
        // ever observing a partially written role document.
        File.Move(temporary, _rolesFile, overwrite: true);
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Der zentrale Rollenpfad ist nicht konfiguriert.");
    }

    private sealed class RoleStoreDocument
    {
        public int SchemaVersion { get; set; } = 1;

        public List<RoleStoreEntry> Roles { get; set; } = new();
    }

    private sealed class RoleStoreEntry
    {
        public string UserName { get; set; } = string.Empty;

        public string Level { get; set; } = string.Empty;
    }
}
