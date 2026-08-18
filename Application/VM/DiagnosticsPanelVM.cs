using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using VIBN_Tools.Core.ViCo;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.Application.VM;

public sealed class DiagnosticsPanelVM : MvvmBase
{
    private readonly ApplicationLogService _log = ApplicationLogService.Instance;

    public DiagnosticsPanelVM()
    {
        ClearCommand = GetCommandBinding(_log.Clear);
        CopyCommand = GetCommandBinding(Copy);
        OpenLogFolderCommand = GetCommandBinding(OpenLogFolder);
    }

    public ObservableCollection<ApplicationLogEntry> Entries => _log.Entries;

    public ICommand ClearCommand { get; }

    public ICommand CopyCommand { get; }

    public ICommand OpenLogFolderCommand { get; }

    private void Copy()
    {
        try
        {
            var text = string.Join(Environment.NewLine, Entries.Select(entry =>
                $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss} | {entry.Level} | {entry.Area} | {entry.Message} | {entry.Details}"));
            Clipboard.SetText(text);
        }
        catch (Exception exception)
        {
            _log.Error("Diagnose", "Das Diagnoseprotokoll konnte nicht kopiert werden.", exception);
        }
    }

    private void OpenLogFolder()
    {
        try
        {
            Directory.CreateDirectory(_log.LogDirectory);
            Process.Start(new ProcessStartInfo(_log.LogDirectory) { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            _log.Error("Diagnose", "Der Logordner konnte nicht geöffnet werden.", exception);
        }
    }
}
