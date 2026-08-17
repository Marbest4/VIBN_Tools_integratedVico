using System.Windows;
using VIBN_Tools.Application.View;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.UiStartup.SmokeTests;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        _ = new System.Windows.Application();
        Services.Initialize();

        var mainWindow = new MainWindow();
        if (mainWindow.DataContext is null)
            throw new InvalidOperationException("MainWindow has no view model.");
        mainWindow.Close();

        Console.WriteLine("The complete WPF main window initialized successfully.");
        return 0;
    }
}
