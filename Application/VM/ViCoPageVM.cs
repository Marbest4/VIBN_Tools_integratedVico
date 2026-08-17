using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using VIBN_Tools.Core.ViCo;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.Application.VM;

public sealed class ViCoPageVM : MvvmBase
{
    private readonly IProjectCatalogService _projectCatalog;
    private readonly IProjectSearchService _projectSearch;
    private readonly IFavoritesRepository _favoritesRepository;
    private readonly IExternalPathLauncher _pathLauncher;
    private readonly IFolderSelectionService _selectionService;
    private IReadOnlyList<ProjectLocation> _allProjects = Array.Empty<ProjectLocation>();
    private bool _initialized;

    public ViCoPageVM(
        IProjectCatalogService projectCatalog,
        IProjectSearchService projectSearch,
        IFavoritesRepository favoritesRepository,
        IExternalPathLauncher pathLauncher,
        IFolderSelectionService selectionService)
    {
        _projectCatalog = projectCatalog;
        _projectSearch = projectSearch;
        _favoritesRepository = favoritesRepository;
        _pathLauncher = pathLauncher;
        _selectionService = selectionService;

        RefreshCommand = GetCommandBindingAsync(RefreshAsync);
        OpenProjectCommand = GetCommandBinding(OpenSelectedProject);
        AddFavoriteCommand = GetCommandBindingAsync(AddSelectedProjectToFavoritesAsync);
        RemoveFavoriteCommand = GetCommandBindingAsync(RemoveSelectedFavoriteAsync);
        OpenFavoriteCommand = GetCommandBinding(OpenSelectedFavorite);
        BrowseFavoriteFolderCommand = GetCommandBinding(BrowseFavoriteFolder);
        BrowseFavoriteFilesCommand = GetCommandBinding(BrowseFavoriteFile);
        SaveFavoriteCommand = GetCommandBindingAsync(SaveFavoriteEditorAsync);
    }

    public ObservableCollection<ProjectLocation> Projects { get; } = new();

    public ObservableCollection<FavoriteEntry> Favorites { get; } = new();

    public ICommand RefreshCommand { get; }

    public ICommand OpenProjectCommand { get; }

    public ICommand AddFavoriteCommand { get; }

    public ICommand RemoveFavoriteCommand { get; }

    public ICommand OpenFavoriteCommand { get; }

    public ICommand BrowseFavoriteFolderCommand { get; }

    public ICommand BrowseFavoriteFilesCommand { get; }

    public ICommand SaveFavoriteCommand { get; }

    private string _favoriteName = string.Empty;
    public string FavoriteName
    {
        get => _favoriteName;
        set
        {
            _favoriteName = value;
            OnPropertyChanged();
        }
    }

    private string _favoritePath = string.Empty;
    public string FavoritePath
    {
        get => _favoritePath;
        set
        {
            _favoritePath = value;
            OnPropertyChanged();
        }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (string.Equals(_searchText, value, StringComparison.Ordinal))
                return;

            _searchText = value;
            OnPropertyChanged();
            ApplySearch();
        }
    }

    private ProjectLocation? _selectedProject;
    public ProjectLocation? SelectedProject
    {
        get => _selectedProject;
        set
        {
            _selectedProject = value;
            OnPropertyChanged();
        }
    }

    private FavoriteEntry? _selectedFavorite;
    public FavoriteEntry? SelectedFavorite
    {
        get => _selectedFavorite;
        set
        {
            _selectedFavorite = value;
            OnPropertyChanged();
            if (value is not null)
            {
                FavoriteName = value.Name;
                FavoritePath = value.FullPath;
            }
        }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            _isBusy = value;
            OnPropertyChanged();
        }
    }

    private string _statusText = "ViCo ist bereit.";
    public string StatusText
    {
        get => _statusText;
        private set
        {
            _statusText = value;
            OnPropertyChanged();
        }
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        _initialized = true;
        await LoadFavoritesAsync();
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        StatusText = "Projektkatalog wird geladen …";

        try
        {
            var snapshot = await _projectCatalog.LoadAsync();
            _allProjects = snapshot.Projects;
            ApplySearch();

            StatusText = snapshot.Warnings.Count == 0
                ? $"{snapshot.Projects.Count} Projekte geladen."
                : $"{snapshot.Projects.Count} Projekte geladen; {snapshot.Warnings.Count} Quelle(n) nicht erreichbar.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplySearch()
    {
        var selectedPath = SelectedProject?.FullPath;
        var matches = _projectSearch.Search(_allProjects, SearchText);

        Projects.Clear();
        foreach (var project in matches)
            Projects.Add(project);

        SelectedProject = Projects.FirstOrDefault(project =>
            string.Equals(project.FullPath, selectedPath, StringComparison.OrdinalIgnoreCase));
    }

    private async Task LoadFavoritesAsync()
    {
        Favorites.Clear();
        foreach (var favorite in await _favoritesRepository.LoadAsync())
            Favorites.Add(favorite);
    }

    private async Task AddSelectedProjectToFavoritesAsync()
    {
        if (SelectedProject is null)
            return;

        if (Favorites.Any(favorite => string.Equals(
                favorite.FullPath,
                SelectedProject.FullPath,
                StringComparison.OrdinalIgnoreCase)))
        {
            StatusText = "Das Projekt ist bereits als Favorit gespeichert.";
            return;
        }

        Favorites.Add(new FavoriteEntry(SelectedProject.DisplayName, SelectedProject.FullPath));
        await SaveFavoritesAsync();
        StatusText = "Favorit gespeichert.";
    }

    private async Task RemoveSelectedFavoriteAsync()
    {
        if (SelectedFavorite is null)
            return;

        Favorites.Remove(SelectedFavorite);
        SelectedFavorite = null;
        await SaveFavoritesAsync();
        StatusText = "Favorit entfernt.";
    }

    private Task SaveFavoritesAsync() =>
        _favoritesRepository.SaveAsync(Favorites.ToArray());

    private void OpenSelectedProject()
    {
        if (SelectedProject is not null)
            _pathLauncher.Open(SelectedProject.FullPath);
    }

    private void OpenSelectedFavorite()
    {
        if (SelectedFavorite is not null)
            _pathLauncher.Open(SelectedFavorite.FullPath);
    }

    private void BrowseFavoriteFolder()
    {
        var selected = _selectionService.SelectFolder("Favoritenordner auswählen", FavoritePath);
        if (selected is null)
            return;

        FavoritePath = selected;
        if (string.IsNullOrWhiteSpace(FavoriteName))
            FavoriteName = new DirectoryInfo(selected).Name;
    }

    private void BrowseFavoriteFile()
    {
        var selected = _selectionService.SelectFiles("Favoritendatei auswählen", Path.GetDirectoryName(FavoritePath))
            .FirstOrDefault();
        if (selected is null)
            return;

        FavoritePath = selected;
        if (string.IsNullOrWhiteSpace(FavoriteName))
            FavoriteName = Path.GetFileNameWithoutExtension(selected);
    }

    private async Task SaveFavoriteEditorAsync()
    {
        if (string.IsNullOrWhiteSpace(FavoriteName) || string.IsNullOrWhiteSpace(FavoritePath))
            return;

        var replacement = new FavoriteEntry(FavoriteName.Trim(), FavoritePath.Trim());
        if (SelectedFavorite is null)
        {
            Favorites.Add(replacement);
        }
        else
        {
            var index = Favorites.IndexOf(SelectedFavorite);
            Favorites[index] = replacement;
        }

        SelectedFavorite = replacement;
        await SaveFavoritesAsync();
        StatusText = "Favorit gespeichert.";
    }
}
