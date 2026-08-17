using System.Diagnostics;
using VIBN_Tools.Core.ViCo;

namespace VIBN_Tools.Infrastructure.ViCo;

public sealed class WindowsPathLauncher : IExternalPathLauncher
{
    public void Open(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Ein Pfad ist erforderlich.", nameof(path));

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}
