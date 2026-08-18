using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.Application.VM
{
    public class MainWindowVM : MvvmBase
    {
        public MainWindowVM()
        {

        }



        public async Task InitializeAsync()
        {
            try
            {
                await ViCoFeatureBootstrapper.InitializeWorkstationDirectoryAsync();
                ApplicationLogService.Instance.Information(
                    "Arbeitsstationen",
                    $"{ViCoFeatureBootstrapper.WorkstationDirectory.Entries.Count - 1} PCs aus dem ViCo-Cache geladen.");
            }
            catch (Exception exception)
            {
                ApplicationLogService.Instance.Error(
                    "Arbeitsstationen",
                    "Die gemeinsame PC-Liste konnte beim Start nicht geladen werden.",
                    exception);
            }
        }
    }
}
