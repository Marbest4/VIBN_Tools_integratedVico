namespace VIBN_Tools.Application;

public interface IFolderSelectionService
{
    string? SelectFolder(string title, string? initialDirectory = null);

    IReadOnlyList<string> SelectFiles(string title, string? initialDirectory = null);
}
