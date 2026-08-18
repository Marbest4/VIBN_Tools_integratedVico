using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VIBN_Tools.Application.View;
using VIBN_Tools.Application.VM;
using VIBN_Tools.Core.ViCo;

namespace VIBN_Tools.UiStartup.SmokeTests;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        _ = new System.Windows.Application();
        var bindingTrace = PresentationTraceSources.DataBindingSource;
        var bindingErrors = new BindingErrorTraceListener();
        bindingTrace.Switch.Level = SourceLevels.Error;
        bindingTrace.Listeners.Add(bindingErrors);

        try
        {
            var workspacePage = new ViCoWorkspacePage();
            ExerciseDeferredTemplates(workspacePage);
            var projectPage = new ViCoPage();
            var projectViewModel = (ViCoPageVM)projectPage.DataContext;
            projectViewModel.Projects.Add(new ProjectLocation("GM1234/05-130", @"C:\Projects\GM1234\05-130"));

            var searchPage = new ViCoSearchPage();
            var searchViewModel = (ViCoSearchPageVM)searchPage.DataContext;
            var workstation = new ViCoWorkstation(
                "GM12345 Tool PC",
                "GM12345",
                "zkds-simulation-p01",
                "TIA Portal V19 | Beckhoff TwinCAT 3",
                "FEE 5",
                "LAN Industrial",
                new[] { "[W] GM1234/05-130 Demo" },
                new[] { "TIA Portal V19", "Beckhoff TwinCAT 3", "Robot: R01 – In Arbeit" },
                new[]
                {
                    new AutomationSoftwareInfo(AutomationPlatform.SiemensTiaPortal, "TIA Portal V19", "TIA Portal V19"),
                    new AutomationSoftwareInfo(AutomationPlatform.BeckhoffTwinCat, "Beckhoff TwinCAT 3", "Beckhoff TwinCAT 3")
                },
                new[] { new ViCoRobotInfo("R01", "In Arbeit", "Robot card") });
            var workstationRow = new ViCoWorkstationRowVM(workstation);
            searchViewModel.Results.Add(workstationRow);
            searchViewModel.SelectedWorkstation = workstationRow;

            var administrationPage = new ViCoAdministrationPage();
            var administrationViewModel = (ViCoAdministrationPageVM)administrationPage.DataContext;
            administrationViewModel.LicenseEntries.Add(new ViCoLicenseEntry(@"grob\user", "Level9", "test"));

            FrameworkElement[] integratedViews =
            [
                projectPage,
                searchPage,
                new ViCoCopyPage(),
                new TiaPortalPage(),
                administrationPage,
                new DiagnosticsPanel()
            ];

            foreach (var view in integratedViews)
            {
                if (view.DataContext is null)
                    throw new InvalidOperationException($"{view.GetType().Name} has no view model.");
                ExerciseDeferredTemplates(view);
            }

            if (Environment.GetEnvironmentVariable("VIBN_CAPTURE_UI_PREVIEW") == "1")
            {
                SavePreview(searchPage, Path.Combine(AppContext.BaseDirectory, "vico-search-preview.png"));
                SavePreview(projectPage, Path.Combine(AppContext.BaseDirectory, "vico-projects-preview.png"));
                SavePreview(workspacePage, Path.Combine(AppContext.BaseDirectory, "vico-workspace-preview.png"));
            }

            Dispatcher.CurrentDispatcher.Invoke(
                static () => { },
                DispatcherPriority.ContextIdle);

            if (bindingErrors.Messages.Count > 0)
            {
                throw new InvalidOperationException(
                    "WPF binding errors were detected:" + Environment.NewLine +
                    string.Join(Environment.NewLine, bindingErrors.Messages));
            }

            Console.WriteLine("All integrated WPF views initialized without binding errors.");
            return 0;
        }
        finally
        {
            bindingTrace.Listeners.Remove(bindingErrors);
        }
    }

    private static void ExerciseDeferredTemplates(FrameworkElement view)
    {
        var size = new Size(1600, 900);
        view.Measure(size);
        view.Arrange(new Rect(size));
        view.UpdateLayout();

        foreach (var dataGrid in FindVisualChildren<DataGrid>(view))
        {
            if (dataGrid.Items.Count == 0)
                continue;
            dataGrid.SelectedIndex = 0;
            dataGrid.ScrollIntoView(dataGrid.Items[0]);
            dataGrid.UpdateLayout();
        }

        Dispatcher.CurrentDispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
                yield return match;
            foreach (var descendant in FindVisualChildren<T>(child))
                yield return descendant;
        }
    }

    private static void SavePreview(FrameworkElement view, string path)
    {
        var bitmap = new RenderTargetBitmap(1600, 900, 96, 96, PixelFormats.Pbgra32);
        var canvas = new DrawingVisual();
        using (var drawing = canvas.RenderOpen())
        {
            drawing.DrawRectangle(Brushes.White, null, new Rect(0, 0, 1600, 900));
            drawing.DrawRectangle(new VisualBrush(view), null, new Rect(0, 0, 1600, 900));
        }
        bitmap.Render(canvas);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private sealed class BindingErrorTraceListener : TraceListener
    {
        public List<string> Messages { get; } = new();

        public override void Write(string? message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                Messages.Add(message);
        }

        public override void WriteLine(string? message) => Write(message);
    }
}
