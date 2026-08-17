using System.Text;
using VIBN_Tools.Core.ViCo;

namespace VIBN_Tools.Infrastructure.ViCo;

public sealed class LegacyTextFavoritesRepository : IFavoritesRepository
{
    private readonly string _filePath;

    public LegacyTextFavoritesRepository(string filePath)
    {
        _filePath = string.IsNullOrWhiteSpace(filePath)
            ? throw new ArgumentException("Ein Favoritenpfad ist erforderlich.", nameof(filePath))
            : filePath;
    }

    public async Task<IReadOnlyList<FavoriteEntry>> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
            return Array.Empty<FavoriteEntry>();

        var lines = await File.ReadAllLinesAsync(_filePath, Encoding.UTF8, cancellationToken);
        var favorites = new List<FavoriteEntry>(lines.Length / 2);

        for (var index = 0; index + 1 < lines.Length; index += 2)
        {
            if (!string.IsNullOrWhiteSpace(lines[index + 1]))
                favorites.Add(new FavoriteEntry(lines[index], lines[index + 1]));
        }

        return favorites;
    }

    public async Task SaveAsync(
        IReadOnlyCollection<FavoriteEntry> favorites,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(favorites);

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var lines = favorites.SelectMany(favorite => new[] { favorite.Name, favorite.FullPath });
        await File.WriteAllLinesAsync(_filePath, lines, Encoding.UTF8, cancellationToken);
    }
}
