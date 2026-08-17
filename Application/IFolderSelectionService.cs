using System.IO;
using Microsoft.Win32;

namespace VIBN_Tools.Application;

public interface IFolderSelectionService
{
    string? SelectFolder(string title, string? initialDirectory = null);

    IReadOnlyList<string> SelectFiles(string title, string? initialDirectory = null);
}

public sealed class WpfFolderSelectionService : IFolderSelectionService
{
    public string? SelectFolder(string title, string? initialDirectory = null)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title,
            Multiselect = false
        };

        if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
            dialog.InitialDirectory = initialDirectory;

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    public IReadOnlyList<string> SelectFiles(string title, string? initialDirectory = null)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Multiselect = true,
            CheckFileExists = true
        };

        if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
            dialog.InitialDirectory = initialDirectory;

        return dialog.ShowDialog() == true ? dialog.FileNames : Array.Empty<string>();
    }
}
