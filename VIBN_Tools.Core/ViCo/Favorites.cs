namespace VIBN_Tools.Core.ViCo;

public sealed record FavoriteEntry(string Name, string FullPath);

public interface IFavoritesRepository
{
    Task<IReadOnlyList<FavoriteEntry>> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(
        IReadOnlyCollection<FavoriteEntry> favorites,
        CancellationToken cancellationToken = default);
}
