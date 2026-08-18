using System.Windows;

namespace VIBN_Tools
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += (_, args) =>
                Application.ApplicationLogService.Instance.Error(
                    "Unbehandelter UI-Fehler",
                    "Die WPF-Oberfläche hat eine unbehandelte Ausnahme ausgelöst.",
                    args.Exception);

            GlobalClasses.Services.Initialize();

        }
    }

}
