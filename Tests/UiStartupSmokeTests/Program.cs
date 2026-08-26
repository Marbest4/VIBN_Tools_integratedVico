using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VIBN_Tools.Application.View;
using VIBN_Tools.Application.VM;
using VIBN_Tools.Core.Kanbanize;
using VIBN_Tools.Core.ViCo;
using VIBN_Tools.Tia.Contracts;

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
                new[] { "[W] GM1234/05-130 Demo", "TIA Portal V19", "Beckhoff TwinCAT 3", "Robot: R01 – In Arbeit" },
                new[]
                {
                    new AutomationSoftwareInfo(AutomationPlatform.SiemensTiaPortal, "TIA Portal V19", "TIA Portal V19"),
                    new AutomationSoftwareInfo(AutomationPlatform.BeckhoffTwinCat, "Beckhoff TwinCAT 3", "Beckhoff TwinCAT 3")
                },
                new[] { new ViCoRobotInfo("R01", "In Arbeit", "Robot card") },
                new ViCoWorkstationConfiguration(
                    710,
                    new ViCoConfigurationField("USER", "zkds-simulation-p01", 711),
                    new ViCoConfigurationField("STANDORT", "Werk 2", 712),
                    new ViCoConfigurationField("SW", "TIA V19 / Beckhoff TwinCAT 3", 713),
                    new ViCoConfigurationField("PROJEKT-IP", "10.20.30.40", 714),
                    new ViCoConfigurationField("SONSTIGES", "Testdaten für die Anleitung", 715)));
            var workstationRow = new ViCoWorkstationRowVM(workstation);
            workstationRow.SetOnline(true);
            workstationRow.SetRemoteSession(new ViCoRemoteSessionInfo(
                true,
                "grob\\operator",
                "grob\\operator",
                new DateTimeOffset(2026, 8, 25, 8, 30, 0, TimeSpan.Zero)));
            searchViewModel.Results.Add(workstationRow);
            searchViewModel.SelectedWorkstation = workstationRow;

            var administrationPage = new ViCoAdministrationPage();
            var administrationViewModel = (ViCoAdministrationPageVM)administrationPage.DataContext;
            administrationViewModel.RoleEntries.Add(new ViCoUserRole(@"grob\user", "Level9", "test"));

            var specialDevicePage = new SpecialDevicePage();
            var specialDeviceViewModel = (SpecialDevicePageVM)specialDevicePage.DataContext;
            specialDeviceViewModel.TiaHardwareRows.Add(new TiaHardwareDeviceRowVM(
                new TiaHardwareModuleInfo
                {
                    Slot = 3,
                    DeviceName = "PLC_1",
                    ModuleName = "Cognex Testmodul",
                    ModuleType = "PROFINET IO device",
                    TypeIdentifier = "TEST-COGNEX",
                    FirmwareVersion = "V2.1",
                    InputStartByte = 20,
                    InputLength = 4,
                    OutputStartByte = 40,
                    OutputLength = 4
                }));

            var kanbanizeCardPage = new KanbanizeCardPage();
            var kanbanizeViewModel = (KanbanizeCardPageVM)kanbanizeCardPage.DataContext;
            // Populate the deferred DataGrid template as well: this catches
            // bindings in the coloured synchronization preview before release.
            kanbanizeViewModel.WorkplaceSynchronization.PreviewItems.Add(
                new VibnWorkplaceSynchronizationRowVM(
                    new VibnWorkplaceSynchronizationItem(
                        VibnWorkplaceSynchronizationAction.Create,
                        new KanbanizeCardInfo(
                            4711,
                            1392,
                            1,
                            2,
                            "[VIBN] Grundinbetriebnahme UI-Prüfung",
                            null,
                            DateTimeOffset.UtcNow),
                        null,
                        "UI-Prüfdatensatz ohne externen Schreibzugriff.",
                        new VibnWorkplaceSchedule(
                            DateTimeOffset.UtcNow.AddDays(-14),
                            DateTimeOffset.UtcNow.AddDays(56)))));

            FrameworkElement[] integratedViews =
            [
                projectPage,
                searchPage,
                new ViCoCopyPage(),
                new TiaPortalPage(),
                administrationPage,
                kanbanizeCardPage,
                specialDevicePage,
                new DiagnosticsPanel()
            ];

            foreach (var view in integratedViews)
            {
                if (view.DataContext is null)
                    throw new InvalidOperationException($"{view.GetType().Name} has no view model.");
                ExerciseDeferredTemplates(view);
            }

            // Manual form, queue and TIA hardware grid now share one page.
            // A populated row catches its ComboBox and converter bindings.
            ExerciseDeferredTemplates(specialDevicePage);

            if (Environment.GetEnvironmentVariable("VIBN_CAPTURE_UI_PREVIEW") == "1")
            {
                SavePreview(searchPage, Path.Combine(AppContext.BaseDirectory, "vico-search-preview.png"));
                SavePreview(projectPage, Path.Combine(AppContext.BaseDirectory, "vico-projects-preview.png"));
                SavePreview(workspacePage, Path.Combine(AppContext.BaseDirectory, "vico-workspace-preview.png"));
                SavePreview(kanbanizeCardPage, Path.Combine(AppContext.BaseDirectory, "kanbanize-cards-preview.png"));
                SavePreview(specialDevicePage, Path.Combine(AppContext.BaseDirectory, "special-devices-preview.png"));
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
